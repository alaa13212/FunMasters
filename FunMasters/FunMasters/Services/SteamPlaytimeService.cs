using System.Text.RegularExpressions;
using FunMasters.Data;
using FunMasters.Shared;
using FunMasters.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

public class SteamPlaytimeService(
    SteamService steamService,
    ApplicationDbContext db,
    AvatarStorage avatarStorage)
{
    private static readonly TimeSpan RefreshCooldown = TimeSpan.FromHours(6);

    public async Task CaptureAllPlaytimesOnFinishAsync(Guid suggestionId)
    {
        var suggestion = await db.Suggestions.FindAsync(suggestionId);
        if (suggestion == null) return;

        var appId = ExtractSteamAppId(suggestion.SteamLink);
        if (appId == null) return;

        var users = await db.Users
            .Where(u => u.SteamId != null)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToHashSet();
        var existing = await db.SteamPlaytimes
            .Where(sp => sp.SuggestionId == suggestionId && userIds.Contains(sp.UserId))
            .ToDictionaryAsync(sp => sp.UserId);

        foreach (var user in users)
            await UpsertPlaytimeAsync(user, suggestion, appId.Value, captureForever: true, existing);
    }

    public async Task<List<SteamPlaytimeDto>> RefreshAllPlaytimesAsync(Guid suggestionId)
    {
        var suggestion = await db.Suggestions.FindAsync(suggestionId);
        if (suggestion == null) return [];

        var appId = ExtractSteamAppId(suggestion.SteamLink);
        if (appId == null) return [];

        var users = await db.Users.Where(u => u.SteamId != null).ToListAsync();
        if (users.Count == 0) return [];

        var userIds = users.Select(u => u.Id).ToHashSet();
        var records = await db.SteamPlaytimes
            .Where(sp => sp.SuggestionId == suggestionId && userIds.Contains(sp.UserId))
            .ToDictionaryAsync(sp => sp.UserId);

        foreach (var user in users)
        {
            records.TryGetValue(user.Id, out var existing);

            if (existing?.ForeverUpdatedAtUtc != null &&
                DateTime.UtcNow - existing.ForeverUpdatedAtUtc.Value < RefreshCooldown)
                continue;

            if (existing == null)
            {
                existing = new SteamPlaytime { SuggestionId = suggestionId, UserId = user.Id, CapturedAtUtc = DateTime.UtcNow };
                db.SteamPlaytimes.Add(existing);
                records[user.Id] = existing;
            }

            try
            {
                var result = await steamService.GetPlaytimeAsync(user.SteamId!, appId.Value);
                if (result == null)
                {
                    existing.ErrorMessage = "Game not found in Steam library or profile is private";
                }
                else
                {
                    // Don't override a higher stored value (e.g. manually entered cross-platform playtime)
                    if (!existing.PlaytimeForeverMinutes.HasValue || result.Value.playtimeForever > existing.PlaytimeForeverMinutes.Value)
                        existing.PlaytimeForeverMinutes = result.Value.playtimeForever;
                    existing.ErrorMessage = null;
                    
                    if(suggestion.Status == SuggestionStatus.Active)
                        existing.Playtime2WeeksMinutes = result.Value.playtime2Weeks;
                }
            }
            catch (Exception ex)
            {
                existing.ErrorMessage = $"Steam API error: {ex.Message}";
            }

            existing.ForeverUpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        foreach (var (userId, record) in records)
            record.User ??= users.First(u => u.Id == userId);

        return records.Values
            .Where(p => p.PlaytimeForeverMinutes > 0)
            .Select(MapToDto).ToList();
    }

    public async Task<List<SteamPlaytimeDto>> GetPlaytimesForSuggestionAsync(Guid suggestionId)
    {
        var playtimes = await db.SteamPlaytimes
            .Include(sp => sp.User)
            .Where(sp => sp.SuggestionId == suggestionId)
            .ToListAsync();

        return playtimes
            .Where(p => p.PlaytimeForeverMinutes > 0)
            .Select(sp => MapToDto(sp)).ToList();
    }

    public async Task<(string? steamId, string? displayName, string? error)> ResolveSteamInputAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (null, null, "Input is empty");

        string? rawId = null;

        var numericUrlMatch = Regex.Match(input, @"steamcommunity\.com/profiles/(\d{17})");
        if (numericUrlMatch.Success)
            rawId = numericUrlMatch.Groups[1].Value;

        if (rawId == null)
        {
            var vanityUrlMatch = Regex.Match(input, @"steamcommunity\.com/id/([^/?\s]+)");
            if (vanityUrlMatch.Success)
            {
                rawId = await steamService.ResolveVanityUrlAsync(vanityUrlMatch.Groups[1].Value);
                if (rawId == null)
                    return (null, null, "Could not resolve Steam vanity URL");
            }
        }

        if (rawId == null && Regex.IsMatch(input.Trim(), @"^\d{17}$"))
            rawId = input.Trim();

        if (rawId == null)
        {
            rawId = await steamService.ResolveVanityUrlAsync(input.Trim());
            if (rawId == null)
                return (null, null, "Could not find a Steam profile with that name or ID");
        }

        var summary = await steamService.GetPlayerSummaryAsync(rawId);
        if (summary == null)
            return (null, null, "Steam profile not found or is private");

        return (rawId, summary.PersonaName, null);
    }

    private async Task UpsertPlaytimeAsync(ApplicationUser user, Suggestion suggestion, int appId, bool captureForever,
        Dictionary<Guid, SteamPlaytime>? preloaded = null)
    {
        SteamPlaytime? existing = null;
        preloaded?.TryGetValue(user.Id, out existing);
        existing ??= await db.SteamPlaytimes
            .FirstOrDefaultAsync(sp => sp.UserId == user.Id && sp.SuggestionId == suggestion.Id);

        if (existing == null)
        {
            existing = new SteamPlaytime
            {
                SuggestionId = suggestion.Id,
                UserId = user.Id,
                CapturedAtUtc = DateTime.UtcNow
            };
            db.SteamPlaytimes.Add(existing);
        }

        try
        {
            var result = await steamService.GetPlaytimeAsync(user.SteamId!, appId);
            if (result == null)
            {
                existing.ErrorMessage = "Game not found in Steam library or profile is private";
            }
            else
            {
                existing.Playtime2WeeksMinutes = result.Value.playtime2Weeks;
                if (captureForever)
                {
                    // Don't override a higher stored value (e.g. manually entered cross-platform playtime)
                    if (!existing.PlaytimeForeverMinutes.HasValue || result.Value.playtimeForever > existing.PlaytimeForeverMinutes.Value)
                        existing.PlaytimeForeverMinutes = result.Value.playtimeForever;
                    existing.ForeverUpdatedAtUtc = DateTime.UtcNow;
                }
                existing.ErrorMessage = null;
            }
        }
        catch (Exception ex)
        {
            existing.ErrorMessage = $"Steam API error: {ex.Message}";
        }

        await db.SaveChangesAsync();
    }

    public SteamPlaytimeDto MapToDto(SteamPlaytime sp) => new()
    {
        UserId = sp.UserId,
        UserName = sp.User?.UserName ?? "",
        AvatarUrl = avatarStorage.GetPublicUrl(sp.UserId),
        SteamId = sp.User?.SteamId,
        PlaytimeForeverMinutes = sp.PlaytimeForeverMinutes,
        Playtime2WeeksMinutes = sp.Playtime2WeeksMinutes,
        ErrorMessage = sp.ErrorMessage
    };

    public static int? ExtractSteamAppId(string? steamLink)
    {
        if (string.IsNullOrWhiteSpace(steamLink)) return null;
        var match = Regex.Match(steamLink, @"store\.steampowered\.com/app/(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out var appId) ? appId : null;
    }
}
