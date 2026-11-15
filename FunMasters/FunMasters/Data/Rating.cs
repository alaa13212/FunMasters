using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Data;

[Index(nameof(SuggestionId), nameof(RaterId), IsUnique = true)]
public class Rating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SuggestionId { get; set; }
    public Suggestion? Suggestion { get; set; }
    public Guid RaterId { get; set; }
    public ApplicationUser? Rater { get; set; }
    
    public int Score { get; set; } // 1..100
    
    [StringLength(50000)]
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When Lucian announced this as a long review. Set once, so a review is never announced twice;
    /// null for every review that has not been, including the long ones written before he listened.
    /// </summary>
    public DateTime? LongReviewAnnouncedAtUtc { get; set; }

    [NotMapped]
    public decimal DecimalScore
    {
        get => Score / 10m;
        set => Score = (int)(value * 10);
    }
    
    public string GetRatingLabel() => RatingUtils.GetRatingLabel(Score);
    
}