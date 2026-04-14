using System.Security.Claims;
using FunMasters.Data;
using FunMasters.Shared;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

public class RatingService(
    ApplicationDbContext db,
    AvatarStorage avatarStorage,
    GameCoverStorage coverStorage,
    LucianGalade lucianGalade,
    IHttpContextAccessor httpContextAccessor) : IRatingApiService
{
    public async Task<List<SuggestionDto>> GetUnratedSuggestionsAsync()
    {
        var userId = GetCurrentUserId();
        ApplicationUser user = (await db.Users.FindAsync(userId))!;

        // Get all finished suggestions
        var finishedSuggestions = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .Include(s => s.Ratings)
            .Where(s => s.Status == SuggestionStatus.Finished && s.FinishedAtUtc > user.RegistrationDateUtc)
            .ToListAsync();

        // Filter to those the user hasn't rated
        var unratedSuggestions = finishedSuggestions
            .Where(s => s.Ratings.All(r => r.RaterId != userId))
            .OrderByDescending(s => s.FinishedAtUtc)
            .ToList();

        return unratedSuggestions.Select(MapToDto).ToList();
    }

    public async Task<List<UserRatingDto>> GetMyRatingsAsync()
    {
        var userId = GetCurrentUserId();
        var ratings = await db.Ratings
            .Include(r => r.Suggestion)
            .ThenInclude(s => s!.SuggestedBy)
            .Where(r => r.RaterId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        var suggestionIds = ratings.Select(r => r.SuggestionId).ToHashSet();
        var playtimes = await db.SteamPlaytimes
            .Where(sp => sp.UserId == userId && suggestionIds.Contains(sp.SuggestionId))
            .ToDictionaryAsync(sp => sp.SuggestionId);

        return ratings.Select(r =>
        {
            playtimes.TryGetValue(r.SuggestionId, out var pt);
            return new UserRatingDto
            {
                RatingId = r.Id,
                Score = r.Score,
                Comment = r.Comment,
                CreatedAtUtc = r.CreatedAtUtc,
                RatingLabel = RatingUtils.GetRatingLabel(r.Score),
                SuggestionId = r.SuggestionId,
                Title = r.Suggestion?.Title ?? "",
                CoverImageUrl = coverStorage.GetPublicUrl(r.SuggestionId),
                FinishedAtUtc = r.Suggestion?.FinishedAtUtc,
                PlaytimeForeverMinutes = pt?.PlaytimeForeverMinutes
            };
        }).ToList();
    }

    public async Task<ApiResult<Guid>> CreateRatingAsync(CreateRatingRequest request)
    {
        var userId = GetCurrentUserId();
        // Check if suggestion exists
        var suggestion = await db.Suggestions.FindAsync(request.SuggestionId);
        if (suggestion == null)
            return ApiResult<Guid>.Fail("Suggestion not found");

        // Check if user already rated this suggestion
        var existingRating = await db.Ratings
            .FirstOrDefaultAsync(r => r.SuggestionId == request.SuggestionId && r.RaterId == userId);

        if (existingRating != null)
            return ApiResult<Guid>.Fail("You have already rated this game");

        // Validate score range (1-100)
        if (request.Score < 1 || request.Score > 100)
            return ApiResult<Guid>.Fail("Score must be between 1 and 100");

        var rating = new Rating
        {
            Id = Guid.NewGuid(),
            SuggestionId = request.SuggestionId,
            RaterId = userId,
            Score = request.Score,
            Comment = request.Comment,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Ratings.Add(rating);
        await db.SaveChangesAsync();

        if (request.ManualPlaytimeMinutes.HasValue)
            await UpsertManualPlaytimeAsync(userId, request.SuggestionId, request.ManualPlaytimeMinutes.Value);

        await CheckAllRatingsInAsync(request.SuggestionId);

        return ApiResult<Guid>.Ok(rating.Id);
    }

    public async Task<ApiResult> UpdateRatingAsync(Guid id, UpdateRatingRequest request)
    {
        var userId = GetCurrentUserId();
        var rating = await db.Ratings.FindAsync(id);
        if (rating == null)
            return ApiResult.Fail("Rating not found");

        // Only owner can update their own rating
        if (rating.RaterId != userId)
            return ApiResult.Fail("You can only update your own ratings");

        // Validate score range (1-100)
        if (request.Score < 1 || request.Score > 100)
            return ApiResult.Fail("Score must be between 1 and 100");

        rating.Score = request.Score;
        rating.Comment = request.Comment;

        await db.SaveChangesAsync();

        if (request.ManualPlaytimeMinutes.HasValue)
            await UpsertManualPlaytimeAsync(userId, rating.SuggestionId, request.ManualPlaytimeMinutes.Value);

        return ApiResult.Ok();
    }

    private async Task CheckAllRatingsInAsync(Guid suggestionId)
    {
        var suggestion = await db.Suggestions
            .Include(s => s.Ratings)
            .FirstOrDefaultAsync(s => s.Id == suggestionId);

        if (suggestion == null || suggestion.Status != SuggestionStatus.Finished)
            return;

        var cutoff = suggestion.ActiveAtUtc ?? suggestion.FinishedAtUtc!.Value;
        var eligibleMemberIds = await db.Users
            .Where(u => u.CycleOrder > 0 && CouncilStatusRoles.MustReview.Contains(u.CouncilStatus) && u.RegistrationDateUtc <= cutoff)
            .Select(u => u.Id)
            .ToListAsync();

        var raterIds = suggestion.Ratings.Select(r => r.RaterId).ToHashSet();
        var unratedCount = eligibleMemberIds.Count(id => !raterIds.Contains(id));

        if (unratedCount > 0) return;

        var avg = suggestion.Ratings.Average(r => r.DecimalScore);
        var label = RatingUtils.GetRatingLabel((int)Math.Round(avg * 10));

        await lucianGalade.SendAllRatingsInAsync(
            suggestion.Title, avg, label, suggestion.Ratings.Count);
    }

    private async Task UpsertManualPlaytimeAsync(Guid userId, Guid suggestionId, int manualMinutes)
    {
        var existing = await db.SteamPlaytimes
            .FirstOrDefaultAsync(sp => sp.UserId == userId && sp.SuggestionId == suggestionId);

        if (existing == null)
        {
            existing = new Data.SteamPlaytime
            {
                SuggestionId = suggestionId,
                UserId = userId,
                CapturedAtUtc = DateTime.UtcNow
            };
            db.SteamPlaytimes.Add(existing);
        }

        // Only set if manually entered value is higher than what is already stored
        if (!existing.PlaytimeForeverMinutes.HasValue || manualMinutes > existing.PlaytimeForeverMinutes.Value)
        {
            existing.PlaytimeForeverMinutes = manualMinutes;
            existing.ErrorMessage = null;
        }

        await db.SaveChangesAsync();
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

    private Guid GetCurrentUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User is not authenticated");
        return userId;
    }
}
