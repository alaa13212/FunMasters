using FunMasters.Shared.DTOs;

namespace FunMasters.Shared.Services;

public interface ILucianApiService
{
    /// <summary>Every trigger with its switch and a sample message, for the Cabinet.</summary>
    Task<LucianCabinetDto> GetCabinetAsync();

    /// <summary>Admin only: turn a trigger on or off.</summary>
    Task<ApiResult> SetTriggerEnabledAsync(string key, bool enabled);

    /// <summary>Any signed-in member: send a custom alert through Lucian, optionally with an image.</summary>
    Task<ApiResult> SendAlertAsync(string text, bool critical, Stream? imageStream, string? imageFileName, string? imageContentType);
}
