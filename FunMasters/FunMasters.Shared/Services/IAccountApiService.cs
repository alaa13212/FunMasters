using FunMasters.Shared.DTOs;

namespace FunMasters.Shared.Services;

public interface IAccountApiService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync();
    Task<ProfileDto?> GetProfileAsync();
    Task<ApiResult> UpdateProfileAsync(UpdateProfileRequest request);
    Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request);
    Task<ApiResult<string>> UploadAvatarAsync(Stream fileStream, string fileName);
    Task<FunMasterProfileDto?> GetFunMasterProfileAsync(Guid userId);
    Task<ApiResult> AddFunMasterCommentAsync(Guid targetUserId, CreateFunMasterCommentRequest request);
    Task<ApiResult> DeleteFunMasterCommentAsync(Guid commentId);
}
