using System.Security.Claims;
using FunMasters.Data;
using FunMasters.Shared;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

public class SuggestionService(
    ApplicationDbContext db,
    GameCoverStorage coverStorage,
    AvatarStorage avatarStorage,
    BadgeStorage badgeStorage,
    QueueManager queueManager,
    SteamPlaytimeService steamPlaytimeService,
    LucianGalade lucianGalade,
    IHttpContextAccessor httpContextAccessor) : ISuggestionApiService
{
    public async Task<HomePageDto> GetHomeDataAsync()
    {
        var all = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .ThenInclude(u => u.UserBadges)
            .ThenInclude(ub => ub.Badge)
            .Include(s => s.Ratings)
            .Where(s => s.Status != SuggestionStatus.Pending)
            .OrderBy(s => s.ActiveAtUtc)
            .ToListAsync();

        var active = all.FirstOrDefault(s => s.Status == SuggestionStatus.Active);
        var queued = all.Where(s => s.Status == SuggestionStatus.Queued).ToList();
        var finished = all.Where(s => s.Status == SuggestionStatus.Finished)
            .OrderByDescending(s => s.FinishedAtUtc)
            .ToList();

        return new HomePageDto
        {
            ActiveSuggestion = active != null ? MapToDto(active) : null,
            QueuedSuggestions = queued.Select(MapToDto).ToList(),
            FinishedSuggestions = finished.Select(MapToDto).ToList()
        };
    }

    public async Task<List<SuggestionDto>> GetFloatingSuggestionsAsync()
    {
        int cycleStart = await db.Suggestions
            .Where(s => s.Status == SuggestionStatus.Queued)
            .OrderByDescending(s => s.ActiveAtUtc)
            .Select(s => s.SuggestedBy!.CycleOrder)
            .FirstOrDefaultAsync();

        var suggestions = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .ThenInclude(u => u.UserBadges)
            .ThenInclude(ub => ub.Badge)
            .Where(s => s.Status == SuggestionStatus.Pending && !s.IsHidden)
            .ToListAsync();

        int maxCycleOrder = suggestions
            .Select(s => s.SuggestedBy)
            .Distinct()
            .Max(u => u!.CycleOrder);

        var activeSuggestionsByUser = suggestions
            .Where(s => s.SuggestedBy!.CycleOrder > 0 && CouncilStatusRoles.CanSuggest.Contains(s.SuggestedBy!.CouncilStatus))
            .GroupBy(s => s.SuggestedById)
            .Select(g => new
            {
                UserId = g.Key,
                User = g.First().SuggestedBy,
                Suggestions = g.ToList()
            })
            .OrderBy(x => x.User!.CycleOrder > cycleStart ? x.User!.CycleOrder - cycleStart : maxCycleOrder + x.User!.CycleOrder - cycleStart)
            .ToList();

        // Excommunicated and Candidate suggestions shown at the end
        var otherSuggestionsByUser = suggestions
            .Where(s => CouncilStatusRoles.ExtraFloatingSuggestions.Contains(s.SuggestedBy!.CouncilStatus))
            .GroupBy(s => s.SuggestedById)
            .Select(g => new
            {
                UserId = g.Key,
                User = g.First().SuggestedBy,
                Suggestions = g.ToList()
            })
            .ToList();

        List<Suggestion> finalSort = [];
        do
        {
            foreach (var userSuggestion in activeSuggestionsByUser)
            {
                Suggestion? suggestion = userSuggestion.Suggestions.MinBy(s => s.Order);
                if (suggestion != null)
                {
                    finalSort.Add(suggestion);
                    userSuggestion.Suggestions.Remove(suggestion);
                }
            }
            
            activeSuggestionsByUser.RemoveAll(group => group.Suggestions.Count == 0);
        } while (activeSuggestionsByUser.Any());

        // Append Excommunicated/Candidate suggestions at the end
        foreach (var userGroup in otherSuggestionsByUser)
        {
            foreach (var suggestion in userGroup.Suggestions.OrderBy(s => s.Order))
            {
                finalSort.Add(suggestion);
            }
        }
        
        return finalSort.Select(MapToDto).ToList();
    }

    public async Task<SuggestionDetailDto?> GetSuggestionDetailsAsync(Guid id)
    {
        var suggestion = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .ThenInclude(u => u.UserBadges)
            .ThenInclude(ub => ub.Badge)
            .Include(s => s.Ratings)
            .ThenInclude(r => r.Rater)
            .ThenInclude(u => u.UserBadges)
            .ThenInclude(ub => ub.Badge)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (suggestion == null) return null;
        var playtimes = await steamPlaytimeService.GetPlaytimesForSuggestionAsync(id);
        return MapToDetailDto(suggestion, playtimes, await LoadGemStateAsync(suggestion));
    }

    public async Task<SuggestionDetailDto?> GetActiveSuggestionAsync()
    {
        var suggestion = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .ThenInclude(u => u.UserBadges)
            .ThenInclude(ub => ub.Badge)
            .Include(s => s.Ratings)
            .ThenInclude(r => r.Rater)
            .ThenInclude(u => u.UserBadges)
            .ThenInclude(ub => ub.Badge)
            .FirstOrDefaultAsync(s => s.Status == SuggestionStatus.Active);

        if (suggestion == null) return null;
        var playtimes = await steamPlaytimeService.GetPlaytimesForSuggestionAsync(suggestion.Id);
        return MapToDetailDto(suggestion, playtimes, await LoadGemStateAsync(suggestion));
    }

    public async Task<List<SuggestionDto>> GetMySuggestionsAsync()
    {
        var userId = GetCurrentUserId();
        var suggestions = await db.Suggestions
            .Where(s => s.SuggestedById == userId)
            .OrderBy(s => s.Order)
            .ToListAsync();

        return suggestions.Select(MapToDto).ToList();
    }
    
    public async Task<SuggestionDto?> GetMySuggestionAsync(Guid id)
    {
        Guid userId = GetCurrentUserId();

        Suggestion? singleOrDefaultAsync = await db.Suggestions
            .Where(s => s.SuggestedById == userId && s.Id == id)
            .SingleOrDefaultAsync();
        
        return singleOrDefaultAsync != null ? MapToDto(singleOrDefaultAsync) : null;
    }

    public async Task<int> GetNextOrderAsync()
    {
        Guid userId = GetCurrentUserId();
        int maxOrder = await db.Suggestions
            .Where(s => s.SuggestedById == userId)
            .Select(s => s.Order)
            .MaxAsync(s => (int?)s) ?? 0;

        return maxOrder + 1;
    }

    public async Task<ApiResult<Guid>> CreateSuggestionAsync(CreateSuggestionRequest request)
    {
        var userId = GetCurrentUserId();
        var suggestion = new Suggestion
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Order = request.Order,
            IsHidden = request.IsHidden,
            SteamLink = request.SteamLink,
            SuggestedById = userId,
            CreatedAtUtc = DateTime.UtcNow,
            Status = SuggestionStatus.Pending
        };

        db.Suggestions.Add(suggestion);
        await db.SaveChangesAsync();

        // Download and save cover art if provided
        if (!string.IsNullOrWhiteSpace(request.CoverArtUrl) &&
            request.CoverArtUrl.Contains("igdb", StringComparison.InvariantCultureIgnoreCase))
        {
            try
            {
                await coverStorage.SaveCoverAsync(request.CoverArtUrl, suggestion.Id);
            }
            catch
            {
                // Cover art download failed, but suggestion is created
            }
        }

        await queueManager.UpdateQueueAsync();

        if (!suggestion.IsHidden)
        {
            var user = await db.Users.FindAsync(userId);
            if (user != null)
                await lucianGalade.SendNewSuggestionAsync(user.UserName ?? "Unknown", request.Title);
        }

        return ApiResult<Guid>.Ok(suggestion.Id);
    }

    public async Task<ApiResult> UpdateSuggestionAsync(Guid id, UpdateSuggestionRequest request)
    {
        var userId = GetCurrentUserId();
        var suggestion = await db.Suggestions.FindAsync(id);
        if (suggestion == null)
            return ApiResult.Fail("Suggestion not found");

        if (suggestion.SuggestedById != userId)
            return ApiResult.Fail("You can only update your own suggestions");

        var wasPending = suggestion.Status == SuggestionStatus.Pending;
        var titleChanged = suggestion.Title != request.Title;

        suggestion.Title = request.Title;
        suggestion.Order = request.Order;
        suggestion.IsHidden = request.IsHidden;
        suggestion.SteamLink = request.SteamLink;

        await db.SaveChangesAsync();

        // Update cover art if provided
        if (!string.IsNullOrWhiteSpace(request.CoverArtUrl) &&
            request.CoverArtUrl.Contains("igdb", StringComparison.InvariantCultureIgnoreCase))
        {
            try
            {
                await coverStorage.SaveCoverAsync(request.CoverArtUrl, suggestion.Id);
            }
            catch
            {
                // Cover art update failed, but suggestion is updated
            }
        }

        await queueManager.UpdateQueueAsync();

        if (wasPending && titleChanged)
        {
            var user = await db.Users.FindAsync(userId);
            if (user != null)
                await lucianGalade.SendSuggestionEditedAsync(user.UserName ?? "Unknown", request.Title);
        }

        return ApiResult.Ok();
    }

    public async Task<ApiResult> DeleteSuggestionAsync(Guid id)
    {
        var userId = GetCurrentUserId();
        var suggestion = await db.Suggestions.FindAsync(id);
        if (suggestion == null)
            return ApiResult.Fail("Suggestion not found");

        // Only owner can delete their own suggestion
        if (suggestion.SuggestedById != userId)
            return ApiResult.Fail("You can only delete your own suggestions");

        var wasPending = suggestion.Status == SuggestionStatus.Pending;
        var user = await db.Users.FindAsync(userId);

        db.Suggestions.Remove(suggestion);
        await db.SaveChangesAsync();

        // Reorder remaining suggestions
        var userSuggestions = await db.Suggestions
            .Where(s => s.SuggestedById == userId)
            .OrderBy(s => s.Order)
            .ToListAsync();

        for (int i = 0; i < userSuggestions.Count; i++)
        {
            userSuggestions[i].Order = i + 1;
        }

        await db.SaveChangesAsync();
        await queueManager.UpdateQueueAsync();

        if (wasPending && user != null)
            await lucianGalade.SendSuggestionDeletedAsync(user.UserName ?? "Unknown");

        return ApiResult.Ok();
    }

    public async Task<ApiResult> ArrangeMySuggestionsAsync(ArrangeSuggestionsRequest request)
    {
        var userId = GetCurrentUserId();
        var pending = await db.Suggestions
            .Where(s => s.SuggestedById == userId && s.Status == SuggestionStatus.Pending)
            .ToListAsync();

        // The client must send back exactly the pending set it was shown. Anything else means the
        // page is stale (a game got queued, added or deleted elsewhere), so refuse rather than guess.
        var arranged = request.VisibleIds.Concat(request.HiddenIds).ToList();
        var pendingById = pending.ToDictionary(s => s.Id);
        if (arranged.Count != pending.Count
            || arranged.Distinct().Count() != arranged.Count
            || !arranged.All(pendingById.ContainsKey))
            return ApiResult.Fail("Your games changed since the page loaded. Reload and try again.");

        // Hand the pending games the order slots they already hold between them, so queued and
        // finished games keep theirs and the queue only ever sees pending games trade places.
        var slots = pending.Select(s => s.Order).Order().ToList();
        var hiddenIds = request.HiddenIds.ToHashSet();
        for (int i = 0; i < arranged.Count; i++)
        {
            var suggestion = pendingById[arranged[i]];
            suggestion.Order = slots[i];
            suggestion.IsHidden = hiddenIds.Contains(suggestion.Id);
        }

        await db.SaveChangesAsync();
        await queueManager.UpdateQueueAsync();
        return ApiResult.Ok();
    }

    /// <summary>
    /// The caller's game that is next to be queued anywhere in the Council, if that happens within
    /// a day. <see cref="QueueManager.RebuildQueueAsync"/> queues a member's top pending game as
    /// soon as they have nothing queued or playing, which in a running rotation only happens when
    /// their active game ends - so the next game queued is always the active suggester's, at the
    /// active game's finish. Answers only that suggester, so nobody learns another member's pick.
    /// </summary>
    public async Task<NextQueuedDto?> GetMyNextQueuedAsync()
    {
        var userId = GetCurrentUserId();
        var active = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .FirstOrDefaultAsync(s => s.Status == SuggestionStatus.Active);

        if (active?.FinishedAtUtc == null || active.SuggestedById != userId)
            return null;

        var timeLeft = active.FinishedAtUtc.Value - FunMastersTime.UtcNow;
        if (timeLeft <= TimeSpan.Zero || timeLeft > TimeSpan.FromHours(24))
            return null;

        // Members outside the rotation never get a game queued.
        var user = active.SuggestedBy!;
        if (user.CycleOrder <= 0 || !CouncilStatusRoles.InQueue.Contains(user.CouncilStatus))
            return null;

        var mine = await db.Suggestions
            .Where(s => s.SuggestedById == userId
                        && (s.Status == SuggestionStatus.Pending || s.Status == SuggestionStatus.Queued))
            .OrderBy(s => s.Order)
            .FirstOrDefaultAsync();

        // Hidden games are queued like any other, so the top one by order counts whether hidden or not.
        return mine is { Status: SuggestionStatus.Pending }
            ? new NextQueuedDto { SuggestionId = mine.Id, QueuedAtUtc = active.FinishedAtUtc.Value }
            : null;
    }

    private SuggestionDto MapToDto(Suggestion suggestion)
    {
        var suggesterBadges = suggestion.SuggestedBy != null
            ? suggestion.SuggestedBy.UserBadges.Select(ub => new UserBadgeDto
            {
                BadgeId = ub.BadgeId,
                Name = ub.Badge.Name,
                Description = ub.Badge.Description,
                ImageUrl = badgeStorage.GetPublicUrl(ub.BadgeId)
            }).ToList()
            : [];

        return new SuggestionDto
        {
            Id = suggestion.Id,
            Title = suggestion.Title,
            IsHidden = suggestion.IsHidden,
            Order = suggestion.Order,
            SuggestedById = suggestion.SuggestedById,
            SuggestedByAvatarUrl = avatarStorage.GetPublicUrl(suggestion.SuggestedById),
            SuggestedByUserName = suggestion.SuggestedBy?.UserName ?? "",
            SuggestedByBadges = suggesterBadges,
            CreatedAtUtc = suggestion.CreatedAtUtc,
            SteamLink = suggestion.SteamLink,
            ActiveAtUtc = suggestion.ActiveAtUtc,
            FinishedAtUtc = suggestion.FinishedAtUtc,
            CycleNumber = suggestion.CycleNumber,
            Status = suggestion.Status,
            AverageRating = suggestion.AverageRating,
            RatingsCount = suggestion.RatingsCount,
            CoverImageUrl = coverStorage.GetPublicUrl(suggestion.Id),
        };
    }

    /// <summary>
    /// Pulls every gem on this title in one round-trip — the row count is bounded by the Council's
    /// size, so fetching them beats two separate aggregates. Yields both the per-review tallies and
    /// which review the viewer already spent their gem on.
    /// </summary>
    private async Task<(Dictionary<Guid, int> CountsByRating, Guid? MyGemRatingId, bool CanAward)>
        LoadGemStateAsync(Suggestion suggestion)
    {
        var gems = await db.Gems
            .Where(g => g.SuggestionId == suggestion.Id)
            .Select(g => new { g.RatingId, g.AwardedById })
            .ToListAsync();

        var countsByRating = gems
            .GroupBy(g => g.RatingId)
            .ToDictionary(g => g.Key, g => g.Count());

        // The game page is open to anonymous visitors, so absence of a viewer is normal.
        var viewerId = TryGetCurrentUserId();
        if (viewerId == null)
            return (countsByRating, null, false);

        var myGemRatingId = gems.FirstOrDefault(g => g.AwardedById == viewerId)?.RatingId;

        // Only members who delivered their own verdict may award, and only once deliberation ended.
        var canAward = suggestion.Status == SuggestionStatus.Finished
                       && myGemRatingId == null
                       && suggestion.Ratings.Any(r => r.RaterId == viewerId);

        return (countsByRating, myGemRatingId, canAward);
    }

    private SuggestionDetailDto MapToDetailDto(
        Suggestion suggestion,
        List<SteamPlaytimeDto> playtimes,
        (Dictionary<Guid, int> CountsByRating, Guid? MyGemRatingId, bool CanAward) gemState)
    {
        var playtimeByUser = playtimes.ToDictionary(p => p.UserId);

        // Sort ratings: those with comments first, then by creation date
        var sortedRatings = suggestion.Ratings
            .OrderByDescending(r => string.IsNullOrWhiteSpace(r.Comment) ? -1 : Math.Clamp(r.Comment?.Length ?? 0, 0, 1))
            .ThenBy(r => r.CreatedAtUtc)
            .ToList();

        var raterIds = sortedRatings.Select(r => r.RaterId).ToHashSet();

        return new SuggestionDetailDto
        {
            Suggestion = MapToDto(suggestion),
            Ratings = sortedRatings.Select(r =>
            {
                playtimeByUser.TryGetValue(r.RaterId, out var pt);
                return new RatingDto
                {
                    Id = r.Id,
                    SuggestionId = r.SuggestionId,
                    RaterId = r.RaterId,
                    RaterUserName = r.Rater?.UserName ?? "",
                    RaterAvatarUrl = avatarStorage.GetPublicUrl(r.RaterId),
                    RaterBadges = r.Rater?.UserBadges.Select(ub => new UserBadgeDto
                    {
                        BadgeId = ub.BadgeId,
                        Name = ub.Badge.Name,
                        Description = ub.Badge.Description,
                        ImageUrl = badgeStorage.GetPublicUrl(ub.BadgeId)
                    }).ToList() ?? [],
                    Score = r.Score,
                    Comment = r.Comment,
                    CreatedAtUtc = r.CreatedAtUtc,
                    RatingLabel = RatingUtils.GetRatingLabel(r.Score),
                    RaterSteamId = r.Rater?.SteamId,
                    PlaytimeForeverMinutes = pt?.PlaytimeForeverMinutes,
                    Playtime2WeeksMinutes = pt?.Playtime2WeeksMinutes,
                    GemCount = gemState.CountsByRating.GetValueOrDefault(r.Id)
                };
            }).ToList(),
            NonReviewerPlaytimes = playtimes
                .Where(p => !raterIds.Contains(p.UserId))
                .ToList(),
            MyGemRatingId = gemState.MyGemRatingId,
            CanAwardGem = gemState.CanAward
        };
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User is not authenticated");
        return userId;
    }

    /// <summary>
    /// Null for an anonymous visitor rather than throwing — the game pages are public.
    /// </summary>
    private Guid? TryGetCurrentUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
