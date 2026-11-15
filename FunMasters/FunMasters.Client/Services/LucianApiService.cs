using System.Net.Http.Json;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;

namespace FunMasters.Client.Services;

public class LucianApiService(HttpClient http) : ILucianApiService
{
    public async Task<LucianCabinetDto> GetCabinetAsync() =>
        await http.GetFromJsonAsync<LucianCabinetDto>("/api/lucian/cabinet") ?? new LucianCabinetDto();

    public async Task<ApiResult> SetTriggerEnabledAsync(string key, bool enabled)
    {
        var response = await http.PutAsJsonAsync($"/api/lucian/triggers/{Uri.EscapeDataString(key)}",
            new SetLucianTriggerRequest { Enabled = enabled });
        return await ReadResultAsync(response, "Failed to switch the trigger");
    }

    public async Task<ApiResult> SendAlertAsync(
        string text, bool critical, Stream? imageStream, string? imageFileName, string? imageContentType)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(text), "text");
        content.Add(new StringContent(critical ? "true" : "false"), "critical");
        if (imageStream != null && !string.IsNullOrEmpty(imageFileName))
        {
            var streamContent = new StreamContent(imageStream);
            if (!string.IsNullOrEmpty(imageContentType))
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageContentType);
            content.Add(streamContent, "image", imageFileName);
        }

        var response = await http.PostAsync("/api/lucian/alert", content);
        return await ReadResultAsync(response, "Failed to send the alert");
    }

    /// An expired session or a forbidden call returns a non-JSON body; report it instead of throwing.
    private static async Task<ApiResult> ReadResultAsync(HttpResponseMessage response, string fallback)
    {
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            return ApiResult.Fail("Your session has expired, or this needs an admin. Sign in again.");

        try
        {
            return await response.Content.ReadFromJsonAsync<ApiResult>() ?? ApiResult.Fail(fallback);
        }
        catch (Exception)
        {
            return ApiResult.Fail(fallback);
        }
    }
}
