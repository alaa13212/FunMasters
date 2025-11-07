using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FunMasters.Data;

public class Suggestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [StringLength(255)]
    public required string Title { get; set; }

    public bool IsHidden { get; set; }
    public int Order { get; set; }
    
    public Guid SuggestedById { get; set; }
    public ApplicationUser? SuggestedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Active window
    public DateTime? ActiveAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    // Rating / cycle
    public int? CycleNumber { get; set; }
    [ForeignKey(nameof(CycleNumber))]
    public Cycle? Cycle { get; set; }
    
    public double? AverageRating => Ratings.Count > 0 ? Ratings.Average(r => r.Score) : null;
    public int RatingsCount => Ratings.Count;

    public ICollection<Rating> Ratings { get; set; } = [];
}