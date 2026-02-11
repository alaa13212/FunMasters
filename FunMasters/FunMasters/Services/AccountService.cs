using System.Security.Claims;
using FunMasters.Data;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.AspNetCore.Identity;

namespace FunMasters.Services;

public class AccountService(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    AvatarStorage avatarStorage,
    IHttpContextAccessor httpContextAccessor) : IAccountApiService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new LoginResponse
            {
                Succeeded = false,
                ErrorMessage = "Invalid email or password"
            };
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: request.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return new LoginResponse
            {
                Succeeded = true,
                RequirePasswordChange = user.RequirePasswordChange
            };
        }

        if (result.IsLockedOut)
        {
            return new LoginResponse
            {
                Succeeded = false,
                IsLockedOut = true,
                ErrorMessage = "Account is locked out"
            };
        }

        return new LoginResponse
        {
            Succeeded = false,
            ErrorMessage = "Invalid email or password"
        };
    }

    public async Task LogoutAsync()
    {
        await signInManager.SignOutAsync();
    }

    public async Task<ProfileDto?> GetProfileAsync()
    {
        var userId = GetCurrentUserId();
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return null;

        return new ProfileDto
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            AvatarUrl = avatarStorage.GetPublicUrl(user.Id)
        };
    }

    public async Task<ApiResult> UpdateProfileAsync(UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return ApiResult.Fail("User not found");

        // Validate
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.UserName))
            return ApiResult.Fail("Email and username are required");

        // Check if email is taken by another user
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null && existingUser.Id != userId)
            return ApiResult.Fail("Email is already in use");

        // Check if username is taken by another user
        existingUser = await userManager.FindByNameAsync(request.UserName);
        if (existingUser != null && existingUser.Id != userId)
            return ApiResult.Fail("Username is already in use");

        // Update user
        user.Email = request.Email;
        user.UserName = request.UserName;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ApiResult.Fail($"Failed to update profile: {errors}");
        }

        // Update security stamp to invalidate old cookies
        await userManager.UpdateSecurityStampAsync(user);

        // Re-sign in the user with updated claims
        await signInManager.RefreshSignInAsync(user);

        return ApiResult.Ok();
    }

    public async Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return ApiResult.Fail("User not found");

        // Validate
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return ApiResult.Fail("Password must be at least 6 characters");

        if (request.NewPassword != request.ConfirmPassword)
            return ApiResult.Fail("Passwords do not match");

        // Change password
        var result = await userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ApiResult.Fail($"Failed to change password: {errors}");
        }

        // Clear RequirePasswordChange flag
        user.RequirePasswordChange = false;
        await userManager.UpdateAsync(user);

        // Re-sign in to update security stamp
        await signInManager.RefreshSignInAsync(user);

        return ApiResult.Ok();
    }

    public async Task<ApiResult<string>> UploadAvatarAsync(Stream fileStream, string fileName)
    {
        var userId = GetCurrentUserId();

        // Validate file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        if (!allowedExtensions.Contains(extension))
            return ApiResult<string>.Fail("Invalid file type. Allowed types: jpg, jpeg, png, gif, webp");

        // Validate file size (max 5MB)
        if (fileStream.Length > 5 * 1024 * 1024)
            return ApiResult<string>.Fail("File size must be less than 5MB");

        try
        {
            var avatarUrl = await avatarStorage.SaveAvatarAsync(fileStream, userId);
            return ApiResult<string>.Ok(avatarUrl);
        }
        catch (Exception ex)
        {
            return ApiResult<string>.Fail($"Failed to upload avatar: {ex.Message}");
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User is not authenticated");
        return userId;
    }
}
