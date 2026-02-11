using FunMasters.Shared.DTOs;

namespace FunMasters.Shared.Services;

public interface IRatingApiService
{
    Task<List<SuggestionDto>> GetUnratedSuggestionsAsync();
    Task<List<UserRatingDto>> GetMyRatingsAsync();
    Task<ApiResult<Guid>> CreateRatingAsync(CreateRatingRequest request);
    Task<ApiResult> UpdateRatingAsync(Guid id, UpdateRatingRequest request);
}
