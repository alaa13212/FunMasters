using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FunMasters.Shared;

namespace FunMasters.Data;

public class Suggestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [StringLength(255)]
    [Required]
    public string Title { get; set; } = null!;

    public bool IsHidden { get; set; }
    public int Order { get; set; }
    
    public Guid SuggestedById { get; set; }
    public ApplicationUser? SuggestedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? SteamLink { get; set; }

    // Active window
    public DateTime? ActiveAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    // Rating / cycle
    public int? CycleNumber { get; set; }
    [ForeignKey(nameof(CycleNumber))]
    public Cycle? Cycle { get; set; }
    
    public SuggestionStatus Status { get; set; }
    public decimal? AverageRating => Ratings.Count > 0 ? Ratings.Average(r => r.DecimalScore) : null;
    public int RatingsCount => Ratings.Count;

    public ICollection<Rating> Ratings { get; set; } = [];

    // Writer of this title: the member whose review on it drew the most gems, named
    // LucianTriggers.TitleWriterDays after it concludes. Null until then, and for titles that
    // concluded before titles had Writers; TitleWriterNamedAtUtc is stamped even when no gem was given.
    public Guid? TitleWriterUserId { get; set; }
    public int? TitleWriterGemCount { get; set; }
    public DateTime? TitleWriterNamedAtUtc { get; set; }

    // Steam discount tracking — price in halalas (Steam minor unit, e.g. 4999 = 49.99 SAR)
    public int? LastKnownPriceSar { get; set; }
}