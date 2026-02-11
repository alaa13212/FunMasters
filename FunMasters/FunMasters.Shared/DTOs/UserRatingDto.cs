namespace FunMasters.Shared.DTOs;

/// <summary>
/// User's rating combined with game/suggestion information
/// </summary>
public class UserRatingDto
{
    // Rating info
    public Guid RatingId { get; set; }
    public int Score { get; set; }
    public decimal DecimalScore => Score / 10m;
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string RatingLabel { get; set; } = null!;

    // Suggestion info
    public Guid SuggestionId { get; set; }
    public string Title { get; set; } = null!;
    public string? CoverImageUrl { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
}
