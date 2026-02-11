using FunMasters.Data;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

public class AdminService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    GameCoverStorage coverStorage,
    AvatarStorage avatarStorage,
    QueueManager queueManager) : IAdminApiService
{
    private const string AdminRole = "Admin";

    // User management
    public async Task<List<UserDto>> GetUsersAsync()
    {
        var users = await userManager.Users
            .OrderBy(u => u.CycleOrder)
            .ToListAsync();

        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                CycleOrder = user.CycleOrder,
                Roles = roles.ToList(),
                AvatarUrl = avatarStorage.GetPublicUrl(user.Id)
            });
        }

        return userDtos;
    }

    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return null;

        var roles = await userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            CycleOrder = user.CycleOrder,
            Roles = roles.ToList(),
            AvatarUrl = avatarStorage.GetPublicUrl(user.Id)
        };
    }

    public async Task<ApiResult<Guid>> CreateUserAsync(CreateUserRequest request)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.UserName))
            return ApiResult<Guid>.Fail("Email and username are required");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return ApiResult<Guid>.Fail("Password must be at least 6 characters");

        // Check if email already exists
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return ApiResult<Guid>.Fail("Email already exists");

        // Check if username already exists
        existingUser = await userManager.FindByNameAsync(request.UserName);
        if (existingUser != null)
            return ApiResult<Guid>.Fail("Username already exists");

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.UserName,
            CycleOrder = request.CycleOrder,
            RequirePasswordChange = false // Admin-created users don't need to change password
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ApiResult<Guid>.Fail($"Failed to create user: {errors}");
        }

        // Add admin role if requested
        if (request.IsAdmin)
        {
            await userManager.AddToRoleAsync(user, AdminRole);
        }

        return ApiResult<Guid>.Ok(user.Id);
    }

    public async Task<ApiResult> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return ApiResult.Fail("User not found");

        // Validate
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.UserName))
            return ApiResult.Fail("Email and username are required");

        // Check if email is taken by another user
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null && existingUser.Id != userId)
            return ApiResult.Fail("Email already exists");

        // Check if username is taken by another user
        existingUser = await userManager.FindByNameAsync(request.UserName);
        if (existingUser != null && existingUser.Id != userId)
            return ApiResult.Fail("Username already exists");

        // Update user properties
        user.Email = request.Email;
        user.UserName = request.UserName;
        user.CycleOrder = request.CycleOrder;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ApiResult.Fail($"Failed to update user: {errors}");
        }

        // Update admin role
        var currentRoles = await userManager.GetRolesAsync(user);
        var isCurrentlyAdmin = currentRoles.Contains(AdminRole);

        if (request.IsAdmin && !isCurrentlyAdmin)
        {
            await userManager.AddToRoleAsync(user, AdminRole);
        }
        else if (!request.IsAdmin && isCurrentlyAdmin)
        {
            await userManager.RemoveFromRoleAsync(user, AdminRole);
        }

        return ApiResult.Ok();
    }

    public async Task<ApiResult> DeleteUserAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return ApiResult.Fail("User not found");

        // Check if user has any suggestions or ratings
        var hasSuggestions = await db.Suggestions.AnyAsync(s => s.SuggestedById == userId);
        var hasRatings = await db.Ratings.AnyAsync(r => r.RaterId == userId);

        if (hasSuggestions || hasRatings)
            return ApiResult.Fail("Cannot delete user with existing suggestions or ratings");

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ApiResult.Fail($"Failed to delete user: {errors}");
        }

        return ApiResult.Ok();
    }

    public async Task<ApiResult> ChangeUserPasswordAsync(Guid userId, AdminChangePasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return ApiResult.Fail("User not found");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return ApiResult.Fail("Password must be at least 6 characters");

        // Remove old password and add new one
        await userManager.RemovePasswordAsync(user);
        var result = await userManager.AddPasswordAsync(user, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ApiResult.Fail($"Failed to change password: {errors}");
        }

        // Clear RequirePasswordChange flag
        user.RequirePasswordChange = false;
        await userManager.UpdateAsync(user);

        return ApiResult.Ok();
    }

    // Suggestion management
    public async Task<List<SuggestionDto>> GetAllSuggestionsAsync()
    {
        var suggestions = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .OrderBy(s => s.Order)
            .ThenBy(s => s.SuggestedBy!.CycleOrder)
            .ToListAsync();

        return suggestions.Select(s => new SuggestionDto
        {
            Id = s.Id,
            Title = s.Title,
            IsHidden = s.IsHidden,
            Order = s.Order,
            SuggestedById = s.SuggestedById,
            SuggestedByAvatarUrl = avatarStorage.GetPublicUrl(s.SuggestedById),
            SuggestedByUserName = s.SuggestedBy?.UserName ?? "",
            CreatedAtUtc = s.CreatedAtUtc,
            SteamLink = s.SteamLink,
            ActiveAtUtc = s.ActiveAtUtc,
            FinishedAtUtc = s.FinishedAtUtc,
            CycleNumber = s.CycleNumber,
            Status = s.Status,
            RatingsCount = s.RatingsCount,
            CoverImageUrl = coverStorage.GetPublicUrl(s.Id),
        }).ToList();
    }

    public async Task<ApiResult> UpdateSuggestionAsync(Guid suggestionId, AdminUpdateSuggestionRequest request)
    {
        var suggestion = await db.Suggestions.FindAsync(suggestionId);
        if (suggestion == null)
            return ApiResult.Fail("Suggestion not found");

        suggestion.Status = request.Status;
        suggestion.ActiveAtUtc = request.ActiveAtUtc;
        suggestion.FinishedAtUtc = request.FinishedAtUtc;
        suggestion.CycleNumber = request.CycleNumber;

        await db.SaveChangesAsync();
        await queueManager.UpdateQueueAsync();

        return ApiResult.Ok();
    }

    // Queue management
    public async Task<ApiResult> RefreshQueueAsync()
    {
        try
        {
            await queueManager.UpdateQueueAsync();
            return ApiResult.Ok();
        }
        catch (Exception ex)
        {
            return ApiResult.Fail($"Failed to refresh queue: {ex.Message}");
        }
    }
}
