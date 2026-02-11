namespace FunMasters.Shared.DTOs;

public class SuggestionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public bool IsHidden { get; set; }
    public int Order { get; set; }
    public Guid SuggestedById { get; set; }
    public string? SuggestedByAvatarUrl { get; set; }
    public string SuggestedByUserName { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public string? SteamLink { get; set; }
    public DateTime? ActiveAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public int? CycleNumber { get; set; }
    public SuggestionStatus Status { get; set; }
    public decimal? AverageRating { get; set; }
    public int RatingsCount { get; set; }
    public string? CoverImageUrl { get; set; }
}
