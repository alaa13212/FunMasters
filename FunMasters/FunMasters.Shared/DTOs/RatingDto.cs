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
}
