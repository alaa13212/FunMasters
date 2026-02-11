using FunMasters.Shared.DTOs;

namespace FunMasters.Shared.Services;

public interface ISuggestionApiService
{
    Task<HomePageDto> GetHomeDataAsync();
    Task<List<SuggestionDto>> GetFloatingSuggestionsAsync();
    Task<SuggestionDetailDto?> GetSuggestionDetailsAsync(Guid id);
    Task<SuggestionDetailDto?> GetActiveSuggestionAsync();
    Task<List<SuggestionDto>> GetMySuggestionsAsync();
    Task<SuggestionDto?> GetMySuggestionAsync(Guid id);
    Task<int> GetNextOrderAsync();
    Task<ApiResult<Guid>> CreateSuggestionAsync(CreateSuggestionRequest request);
    Task<ApiResult> UpdateSuggestionAsync(Guid id, UpdateSuggestionRequest request);
    Task<ApiResult> DeleteSuggestionAsync(Guid id);
    Task<ApiResult> ReorderSuggestionAsync(ReorderSuggestionRequest request);
}
