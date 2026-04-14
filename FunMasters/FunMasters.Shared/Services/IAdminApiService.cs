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
    Task<ApiResult> FinishEarlyAsync(Guid id);

    // Badges
    Task<List<BadgeDto>> GetBadgesAsync();
    Task<ApiResult<Guid>> CreateBadgeAsync(string name, string? description, Stream? fileStream, string? fileName);
    Task<ApiResult> UpdateBadgeAsync(Guid id, UpdateBadgeRequest request);
    Task<ApiResult> UploadBadgeImageAsync(Guid id, Stream fileStream, string fileName);
    Task<ApiResult> DeleteBadgeAsync(Guid id);
    Task<ApiResult> AssignBadgeAsync(Guid userId, Guid badgeId);
    Task<ApiResult> RemoveBadgeAsync(Guid userId, Guid badgeId);
}
