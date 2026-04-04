namespace FunMasters.Shared.DTOs;

public class SteamPlaytimeDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? SteamId { get; set; }
    public int? PlaytimeForeverMinutes { get; set; }
    public int? Playtime2WeeksMinutes { get; set; }
    public string? ErrorMessage { get; set; }

    public string? SteamProfileUrl => SteamId != null ? $"https://steamcommunity.com/profiles/{SteamId}" : null;

    public bool HasData => PlaytimeForeverMinutes.HasValue || Playtime2WeeksMinutes.HasValue;

    public string PlaytimeForeverDisplay => PlaytimeUtils.FormatMinutes(PlaytimeForeverMinutes);
    public string Playtime2WeeksDisplay => PlaytimeUtils.FormatMinutes(Playtime2WeeksMinutes);
}
