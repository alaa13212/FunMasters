using System.Security.Claims;
using FunMasters.Data;
using FunMasters.Shared;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

public class LucianCabinetService(
    ApplicationDbContext db,
    LucianGalade lucian,
    IHttpContextAccessor httpContextAccessor,
    ILogger<LucianCabinetService> logger) : ILucianApiService
{
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };

    private const long MaxImageBytes = 5 * 1024 * 1024;

    // Telegram caps photo captions at 1024 characters and plain messages at 4096.
    private const int CaptionLimit = 1024;
    private const int MessageLimit = 4096;

    public async Task<LucianCabinetDto> GetCabinetAsync()
    {
        var switches = await db.LucianTriggerSettings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Enabled);

        var samples = LucianSamples.Build(lucian.TitleLink(Guid.Parse("5a3c1e0b-7d42-4f9a-9b1e-2c6d8e4f0a17")));
        var viewer = await GetCurrentUserAsync();

        return new LucianCabinetDto
        {
            CanSendAlert = viewer != null && MaySendAlerts(viewer.Value.Status),
            Triggers = LucianTriggers.All.Select(t => new LucianTriggerDto
            {
                Key = t.Key,
                Name = t.Name,
                WhenItFires = t.WhenItFires,
                Form = t.Form,
                Enabled = switches.GetValueOrDefault(t.Key, true),
                SampleHtml = samples.GetValueOrDefault(t.Key, "")
            }).ToList()
        };
    }

    public async Task<ApiResult> SetTriggerEnabledAsync(string key, bool enabled)
    {
        if (!LucianTriggers.Exists(key))
            return ApiResult.Fail("Unknown trigger");

        var setting = await db.LucianTriggerSettings.FindAsync(key);
        if (setting == null)
        {
            setting = new LucianTriggerSetting { Key = key };
            db.LucianTriggerSettings.Add(setting);
        }

        setting.Enabled = enabled;
        setting.UpdatedAtUtc = FunMastersTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("Lucian's {Trigger} trigger switched {State} by {User}",
            key, enabled ? "on" : "off", httpContextAccessor.HttpContext?.User.Identity?.Name);
        return ApiResult.Ok();
    }

    public async Task<ApiResult> SendAlertAsync(
        string text, bool critical, Stream? imageStream, string? imageFileName, string? imageContentType)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ApiResult.Fail("Write the alert first");

        var viewer = await GetCurrentUserAsync();
        if (viewer == null)
            return ApiResult.Fail("You must be signed in to send an alert");

        if (!MaySendAlerts(viewer.Value.Status))
            return ApiResult.Fail("The Council no longer hears from those it has cast out.");

        var sender = viewer.Value.UserName;

        if (imageStream != null)
        {
            if (string.IsNullOrWhiteSpace(imageContentType) || !AllowedImageContentTypes.Contains(imageContentType))
                return ApiResult.Fail("Unsupported image type. Allowed: JPEG, PNG, GIF, WebP");

            if (imageStream.Length > MaxImageBytes)
                return ApiResult.Fail("Image must be 5 MB or smaller");
        }

        // Judged on the framed message, since the opener, sign-off and urgent line count too.
        var message = LucianGalade.BuildAlertMessage(sender, text, critical);
        var limit = imageStream != null ? CaptionLimit : MessageLimit;
        if (message.Length > limit)
            return ApiResult.Fail(imageStream != null
                ? $"With an image, the alert must fit in {CaptionLimit} characters including Lucian's frame; it is {message.Length}"
                : $"The alert must fit in {MessageLimit} characters including Lucian's frame; it is {message.Length}");

        try
        {
            await lucian.SendAlertAsync(message, imageStream, imageFileName, imageContentType);
            logger.LogInformation("Custom alert sent through Lucian by {Sender}", sender);
            return ApiResult.Ok();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send a custom alert for {Sender}", sender);
            return ApiResult.Fail($"Lucian could not deliver the alert: {ex.Message}");
        }
    }

    /// <summary>Excommunicated and executed members may not speak through Lucian.</summary>
    private static bool MaySendAlerts(CouncilStatus status) => !CouncilStatusRoles.CastOut.Contains(status);

    private async Task<(string UserName, CouncilStatus Status)?> GetCurrentUserAsync()
    {
        var idClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (idClaim == null || !Guid.TryParse(idClaim, out var userId))
            return null;

        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.UserName, u.CouncilStatus })
            .FirstOrDefaultAsync();

        return user == null ? null : (user.UserName ?? "Unknown", user.CouncilStatus);
    }
}
