using System.Net.Http.Json;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;

namespace FunMasters.Client.Services;

public class AdminApiService(HttpClient http) : IAdminApiService
{
    // User management
    public async Task<List<UserDto>> GetUsersAsync()
    {
        return await http.GetFromJsonAsync<List<UserDto>>("/api/admin/users")
            ?? [];
    }

    public async Task<UserDto?> GetUserAsync(Guid id)
    {
        return await http.GetFromJsonAsync<UserDto>($"/api/admin/users/{id}");
    }

    public async Task<ApiResult<Guid>> CreateUserAsync(CreateUserRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/admin/users", request);
        return await response.Content.ReadFromJsonAsync<ApiResult<Guid>>()
            ?? ApiResult<Guid>.Fail("Failed to create user");
    }

    public async Task<ApiResult> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/admin/users/{id}", request);
        return await response.Content.ReadFromJsonAsync<ApiResult>()
            ?? ApiResult.Fail("Failed to update user");
    }

    public async Task<ApiResult> DeleteUserAsync(Guid id)
    {
        var response = await http.DeleteAsync($"/api/admin/users/{id}");
        return await response.Content.ReadFromJsonAsync<ApiResult>()
            ?? ApiResult.Fail("Failed to delete user");
    }

    public async Task<ApiResult> ChangeUserPasswordAsync(Guid id, AdminChangePasswordRequest request)
    {
        var response = await http.PostAsJsonAsync($"/api/admin/users/{id}/change-password", request);
        return await response.Content.ReadFromJsonAsync<ApiResult>()
            ?? ApiResult.Fail("Failed to change password");
    }

    // Suggestion management
    public async Task<List<SuggestionDto>> GetAllSuggestionsAsync()
    {
        return await http.GetFromJsonAsync<List<SuggestionDto>>("/api/admin/suggestions")
            ?? [];
    }

    public async Task<ApiResult> UpdateSuggestionAsync(Guid id, AdminUpdateSuggestionRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/admin/suggestions/{id}", request);
        return await response.Content.ReadFromJsonAsync<ApiResult>()
            ?? ApiResult.Fail("Failed to update suggestion");
    }

    // Queue management
    public async Task<ApiResult> RefreshQueueAsync()
    {
        var response = await http.PostAsync("/api/admin/queue/refresh", null);
        return await response.Content.ReadFromJsonAsync<ApiResult>()
            ?? ApiResult.Fail("Failed to refresh queue");
    }
}
