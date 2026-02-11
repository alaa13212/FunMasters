using FunMasters.Shared.DTOs;

namespace FunMasters.Shared.Services;

public interface IAdminApiService
{
    // Users
    Task<List<UserDto>> GetUsersAsync();
    Task<UserDto?> GetUserAsync(Guid id);
    Task<ApiResult<Guid>> CreateUserAsync(CreateUserRequest request);
    Task<ApiResult> UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task<ApiResult> DeleteUserAsync(Guid id);
    Task<ApiResult> ChangeUserPasswordAsync(Guid id, AdminChangePasswordRequest request);

    // Suggestions
    Task<List<SuggestionDto>> GetAllSuggestionsAsync();
    Task<ApiResult> UpdateSuggestionAsync(Guid id, AdminUpdateSuggestionRequest request);
    Task<ApiResult> RefreshQueueAsync();
}
