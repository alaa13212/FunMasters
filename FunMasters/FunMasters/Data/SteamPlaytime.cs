namespace FunMasters.Data;

public class SteamPlaytime
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SuggestionId { get; set; }
    public Suggestion? Suggestion { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Captured at game finish — immutable snapshot of the 2-week window
    public int? Playtime2WeeksMinutes { get; set; }

    // Refreshable on demand
    public int? PlaytimeForeverMinutes { get; set; }

    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ForeverUpdatedAtUtc { get; set; }

    // null = success; non-null describes why playtime could not be retrieved
    public string? ErrorMessage { get; set; }
}
