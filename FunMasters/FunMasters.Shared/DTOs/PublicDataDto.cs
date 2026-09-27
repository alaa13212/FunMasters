namespace FunMasters.Shared.DTOs;

/// <summary>
/// Everything the Council holds that is not private, in one read.
/// </summary>
/// <remarks>
/// Deliberately narrower than the database. No email addresses, no password material, no profile
/// drafts, no badges beyond what a profile already shows. A member who wants their own row gone
/// can ask; a visitor must not be handed what nobody consented to publish.
/// </remarks>
public class PublicDataDto
{
    public List<PublicUserRow> Users { get; set; } = [];
    public List<PublicSuggestionRow> Suggestions { get; set; } = [];
    public List<PublicRatingRow> Ratings { get; set; } = [];
    public List<PublicPlaytimeRow> Playtimes { get; set; } = [];
}

public class PublicUserRow
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public CouncilStatus CouncilStatus { get; set; }
    public int CycleOrder { get; set; }
    public DateTime RegistrationDateUtc { get; set; }
    public string? SteamId { get; set; }
}

/// <summary>
/// A title's public standing. Only the window the Council gave it is published - not when it was
/// proposed, ordered, or who proposed it, which is draft bookkeeping rather than public record.
/// </summary>
public class PublicSuggestionRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public SuggestionStatus Status { get; set; }
    public DateTime? ActiveAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
}

public class PublicRatingRow
{
    public Guid Id { get; set; }
    public Guid SuggestionId { get; set; }
    public string? SuggestionTitle { get; set; }
    public Guid RaterId { get; set; }
    public string? RaterUserName { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class PublicPlaytimeRow
{
    public Guid SuggestionId { get; set; }
    public string? SuggestionTitle { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public int? PlaytimeForeverMinutes { get; set; }
    public int? Playtime2WeeksMinutes { get; set; }
    public DateTime CapturedAtUtc { get; set; }
}
