using System.Net.Http.Json;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;

namespace FunMasters.Client.Services;

public class AccountApiService(HttpClient http) : IAccountApiService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/account/login", request);
        return await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? new LoginResponse { Succeeded = false, ErrorMessage = "Failed to parse response" };
    }

    public async Task LogoutAsync()
    {
        await http.PostAsync("/api/account/logout", null);
    }

    public async Task<ProfileDto?> GetProfileAsync()
    {
        return await http.GetFromJsonAsync<ProfileDto>("/api/account/profile");
    }

    public async Task<ApiResult> UpdateProfileAsync(UpdateProfileRequest request)
    {
        var response = await http.PutAsJsonAsync("/api/account/profile", request);
        return await response.Content.ReadFromJsonAsync<ApiResult>()
            ?? ApiResult.Fail("Failed to update profile");
    }

    public async Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/account/change-password", request);
        return await response.Content.ReadFromJsonAsync<ApiResult>()
            ?? ApiResult.Fail("Failed to change password");
    }

    public async Task<ApiResult<string>> UploadAvatarAsync(Stream fileStream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", fileName);

        var response = await http.PostAsync("/api/account/upload-avatar", content);
        return await response.Content.ReadFromJsonAsync<ApiResult<string>>()
            ?? ApiResult<string>.Fail("Failed to upload avatar");
    }
}
