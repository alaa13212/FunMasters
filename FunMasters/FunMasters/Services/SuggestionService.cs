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
    QueueManager queueManager,
    IHttpContextAccessor httpContextAccessor) : ISuggestionApiService
{
    public async Task<HomePageDto> GetHomeDataAsync()
    {
        var all = await db.Suggestions
            .Include(s => s.SuggestedBy)
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
            .Where(s => s.Status == SuggestionStatus.Pending && !s.IsHidden)
            .ToListAsync();

        int maxCycleOrder = suggestions
            .Select(s => s.SuggestedBy)
            .Distinct()
            .Select(u => u!.CycleOrder)
            .DefaultIfEmpty(0)
            .Max();

        // Sort by cycle order starting from cycleStart
        var sorted = suggestions
            .OrderBy(s => (s.SuggestedBy!.CycleOrder - cycleStart + maxCycleOrder + 1) % (maxCycleOrder + 1))
            .ThenBy(s => s.Order)
            .ToList();

        return sorted.Select(MapToDto).ToList();
    }

    public async Task<SuggestionDetailDto?> GetSuggestionDetailsAsync(Guid id)
    {
        var suggestion = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .Include(s => s.Ratings)
            .ThenInclude(r => r.Rater)
            .FirstOrDefaultAsync(s => s.Id == id);

        return suggestion != null ? MapToDetailDto(suggestion) : null;
    }

    public async Task<SuggestionDetailDto?> GetActiveSuggestionAsync()
    {
        var suggestion = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .Include(s => s.Ratings)
            .ThenInclude(r => r.Rater)
            .FirstOrDefaultAsync(s => s.Status == SuggestionStatus.Active);

        return suggestion != null ? MapToDetailDto(suggestion) : null;
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

        return ApiResult<Guid>.Ok(suggestion.Id);
    }

    public async Task<ApiResult> UpdateSuggestionAsync(Guid id, UpdateSuggestionRequest request)
    {
        var userId = GetCurrentUserId();
        var suggestion = await db.Suggestions.FindAsync(id);
        if (suggestion == null)
            return ApiResult.Fail("Suggestion not found");

        // Only owner can update their own suggestion
        if (suggestion.SuggestedById != userId)
            return ApiResult.Fail("You can only update your own suggestions");

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

        return ApiResult.Ok();
    }

    public async Task<ApiResult> ReorderSuggestionAsync(ReorderSuggestionRequest request)
    {
        var userId = GetCurrentUserId();
        var suggestion = await db.Suggestions.FindAsync(request.SuggestionId);
        if (suggestion == null)
            return ApiResult.Fail("Suggestion not found");

        // Only owner can reorder their own suggestion
        if (suggestion.SuggestedById != userId)
            return ApiResult.Fail("You can only reorder your own suggestions");

        // Only pending suggestions can be reordered
        if (suggestion.Status != SuggestionStatus.Pending)
            return ApiResult.Fail("Can only reorder pending suggestions");

        List<Suggestion> sortGames;
        if (request.Direction.Equals("up", StringComparison.OrdinalIgnoreCase))
        {
            sortGames = await db.Suggestions
                .OrderByDescending(g => g.Order)
                .Where(s => s.SuggestedById == userId && s.Order <= suggestion.Order && s.Status == SuggestionStatus.Pending)
                .Take(2)
                .ToListAsync();

            if (sortGames.Count < 2)
                return ApiResult.Fail("Cannot move up");

            var thisGame = sortGames[0];
            var otherGame = sortGames[1];
            otherGame.Order++;
            thisGame.Order--;
        }
        else if (request.Direction.Equals("down", StringComparison.OrdinalIgnoreCase))
        {
            sortGames = await db.Suggestions
                .OrderBy(g => g.Order)
                .Where(s => s.SuggestedById == userId && s.Order >= suggestion.Order && s.Status == SuggestionStatus.Pending)
                .Take(2)
                .ToListAsync();

            if (sortGames.Count < 2)
                return ApiResult.Fail("Cannot move down");

            var thisGame = sortGames[0];
            var otherGame = sortGames[1];
            otherGame.Order--;
            thisGame.Order++;
        }
        else
        {
            return ApiResult.Fail("Invalid direction");
        }

        await db.SaveChangesAsync();
        return ApiResult.Ok();
    }

    private SuggestionDto MapToDto(Suggestion suggestion)
    {
        return new SuggestionDto
        {
            Id = suggestion.Id,
            Title = suggestion.Title,
            IsHidden = suggestion.IsHidden,
            Order = suggestion.Order,
            SuggestedById = suggestion.SuggestedById,
            SuggestedByAvatarUrl = avatarStorage.GetPublicUrl(suggestion.SuggestedById),
            SuggestedByUserName = suggestion.SuggestedBy?.UserName ?? "",
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

    private SuggestionDetailDto MapToDetailDto(Suggestion suggestion)
    {
        // Sort ratings: those with comments first, then by creation date
        var sortedRatings = suggestion.Ratings
            .OrderByDescending(r => string.IsNullOrWhiteSpace(r.Comment) ? -1 : Math.Clamp(r.Comment?.Length ?? 0, 0, 1))
            .ThenBy(r => r.CreatedAtUtc)
            .ToList();

        return new SuggestionDetailDto
        {
            Suggestion = MapToDto(suggestion),
            Ratings = sortedRatings.Select(r => new RatingDto
            {
                Id = r.Id,
                SuggestionId = r.SuggestionId,
                RaterId = r.RaterId,
                RaterUserName = r.Rater?.UserName ?? "",
                RaterAvatarUrl = avatarStorage.GetPublicUrl(r.RaterId),
                Score = r.Score,
                Comment = r.Comment,
                CreatedAtUtc = r.CreatedAtUtc,
                RatingLabel = RatingUtils.GetRatingLabel(r.Score)
            }).ToList()
        };
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User is not authenticated");
        return userId;
    }
}
