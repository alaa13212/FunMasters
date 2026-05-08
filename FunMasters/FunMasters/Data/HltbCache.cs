namespace FunMasters.Data;

public class HltbCache
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The game title used as the search key (normalized to lowercase for case-insensitive matching).
    /// </summary>
    public string Title { get; set; } = null!;

    public string? MainStory { get; set; }
    public string? MainPlusExtras { get; set; }
    public string? Completionist { get; set; }
    public string? GameUrl { get; set; }

    /// <summary>
    /// When this cache entry expires. New games (< 3 months) expire after 1 day, older games after 1 month.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
