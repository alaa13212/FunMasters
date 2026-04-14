using System.Security.Claims;
using FunMasters.Data;
using FunMasters.Shared;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

public class AccountService(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    AvatarStorage avatarStorage,
    BadgeStorage badgeStorage,
    GameCoverStorage coverStorage,
    ApplicationDbContext db,
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
            AvatarUrl = avatarStorage.GetPublicUrl(user.Id),
            SteamId = user.SteamId,
            Bio = user.Bio
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

        if (!string.IsNullOrWhiteSpace(request.SteamId) &&
            !System.Text.RegularExpressions.Regex.IsMatch(request.SteamId, @"^\d{17}$"))
            return ApiResult.Fail("Invalid Steam ID format. Please use the Verify button to look up your Steam ID.");

        // Update user
        user.Email = request.Email;
        user.UserName = request.UserName;
        user.SteamId = string.IsNullOrWhiteSpace(request.SteamId) ? null : request.SteamId;
        user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio;

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

    public async Task<FunMasterProfileDto?> GetFunMasterProfileAsync(Guid userId)
    {
        var user = await db.Users
            .Include(u => u.UserBadges)
            .ThenInclude(ub => ub.Badge)
            .Include(u => u.ReceivedComments)
            .ThenInclude(c => c.Author)
            .ThenInclude(a => a!.UserBadges)
            .ThenInclude(ub => ub.Badge)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        // Suggested games
        var suggestedGames = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .Include(s => s.Ratings)
            .Where(s => s.SuggestedById == userId && s.Status != SuggestionStatus.Pending)
            .OrderByDescending(s => s.FinishedAtUtc)
            .Take(20)
            .ToListAsync();

        // Reviewed games
        var reviewedRatings = await db.Ratings
            .Include(r => r.Suggestion)
            .ThenInclude(s => s!.SuggestedBy)
            .Where(r => r.RaterId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(20)
            .ToListAsync();

        return new FunMasterProfileDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            Bio = user.Bio,
            AvatarUrl = avatarStorage.GetPublicUrl(user.Id),
            CouncilStatus = user.CouncilStatus.ToString(),
            Badges = user.UserBadges.Select(ub => new UserBadgeDto
            {
                BadgeId = ub.BadgeId,
                Name = ub.Badge.Name,
                Description = ub.Badge.Description,
                ImageUrl = badgeStorage.GetPublicUrl(ub.BadgeId)
            }).ToList(),
            SuggestedGames = suggestedGames.Select(MapToSuggestionDto).ToList(),
            ReviewedGames = reviewedRatings.Select(r => new UserRatingDto
            {
                RatingId = r.Id,
                Score = r.Score,
                Comment = r.Comment,
                CreatedAtUtc = r.CreatedAtUtc,
                RatingLabel = RatingUtils.GetRatingLabel(r.Score),
                SuggestionId = r.SuggestionId,
                Title = r.Suggestion?.Title ?? "",
                CoverImageUrl = coverStorage.GetPublicUrl(r.SuggestionId)
            }).ToList(),
            Comments = user.ReceivedComments
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(c => new FunMasterCommentDto
                {
                    Id = c.Id,
                    AuthorId = c.AuthorId,
                    AuthorUserName = c.Author.UserName ?? "",
                    AuthorAvatarUrl = avatarStorage.GetPublicUrl(c.AuthorId),
                    AuthorBadges = c.Author.UserBadges.Select(ub => new UserBadgeDto
                    {
                        BadgeId = ub.BadgeId,
                        Name = ub.Badge.Name,
                        Description = ub.Badge.Description,
                        ImageUrl = badgeStorage.GetPublicUrl(ub.BadgeId)
                    }).ToList(),
                    Text = c.Text,
                    CreatedAtUtc = c.CreatedAtUtc
                }).ToList()
        };
    }

    public async Task<ApiResult> AddFunMasterCommentAsync(Guid targetUserId, CreateFunMasterCommentRequest request)
    {
        var authorId = GetCurrentUserId();
        if (authorId == targetUserId)
            return ApiResult.Fail("Cannot comment on your own profile");

        var target = await db.Users.FindAsync(targetUserId);
        if (target == null)
            return ApiResult.Fail("User not found");

        if (string.IsNullOrWhiteSpace(request.Text))
            return ApiResult.Fail("Comment cannot be empty");

        if (request.Text.Length > 1000)
            return ApiResult.Fail("Comment cannot exceed 1000 characters");

        var comment = new FunMasterComment
        {
            TargetUserId = targetUserId,
            AuthorId = authorId,
            Text = request.Text.Trim()
        };

        db.FunMasterComments.Add(comment);
        await db.SaveChangesAsync();

        return ApiResult.Ok();
    }

    public async Task<ApiResult> DeleteFunMasterCommentAsync(Guid commentId)
    {
        var userId = GetCurrentUserId();
        var comment = await db.FunMasterComments.FindAsync(commentId);
        if (comment == null)
            return ApiResult.Fail("Comment not found");

        // Only the author or an admin can delete a comment
        if (comment.AuthorId != userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null || !await userManager.IsInRoleAsync(user, "Admin"))
                return ApiResult.Fail("You can only delete your own comments");
        }

        db.FunMasterComments.Remove(comment);
        await db.SaveChangesAsync();

        return ApiResult.Ok();
    }

    private SuggestionDto MapToSuggestionDto(Suggestion suggestion)
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
