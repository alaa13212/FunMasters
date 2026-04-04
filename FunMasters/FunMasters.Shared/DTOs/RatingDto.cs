namespace FunMasters.Shared.DTOs;

public class RatingDto
{
    public Guid Id { get; set; }
    public Guid SuggestionId { get; set; }
    public Guid RaterId { get; set; }
    public string RaterUserName { get; set; } = null!;
    public string? RaterAvatarUrl { get; set; }
    public int Score { get; set; }
    public decimal DecimalScore => Score / 10m;
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string RatingLabel { get; set; } = null!;
    public string? RaterSteamId { get; set; }
    public string? RaterSteamProfileUrl => RaterSteamId != null ? $"https://steamcommunity.com/profiles/{RaterSteamId}" : null;
    public int? PlaytimeForeverMinutes { get; set; }
    public int? Playtime2WeeksMinutes { get; set; }
    public string PlaytimeForeverDisplay => PlaytimeUtils.FormatMinutes(PlaytimeForeverMinutes);
    public string Playtime2WeeksDisplay => PlaytimeUtils.FormatMinutes(Playtime2WeeksMinutes);
}
