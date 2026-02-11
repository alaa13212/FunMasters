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

    [NotMapped]
    public decimal DecimalScore
    {
        get => Score / 10m;
        set => Score = (int)(value * 10);
    }
    
    public string GetRatingLabel() => RatingUtils.GetRatingLabel(Score);
    
}