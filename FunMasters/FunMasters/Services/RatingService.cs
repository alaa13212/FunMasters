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
            .ThenInclude(s => s.SuggestedBy)
            .Where(r => r.RaterId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        return ratings.Select(r => new UserRatingDto
        {
            RatingId = r.Id,
            Score = r.Score,
            Comment = r.Comment,
            CreatedAtUtc = r.CreatedAtUtc,
            RatingLabel = RatingUtils.GetRatingLabel(r.Score),
            SuggestionId = r.SuggestionId,
            Title = r.Suggestion?.Title ?? "",
            CoverImageUrl = coverStorage.GetPublicUrl(r.SuggestionId),
            FinishedAtUtc = r.Suggestion?.FinishedAtUtc
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

    private Guid GetCurrentUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User is not authenticated");
        return userId;
    }
}
