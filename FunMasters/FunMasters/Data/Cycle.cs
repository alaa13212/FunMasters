using Microsoft.EntityFrameworkCore;

namespace FunMasters.Data;

[PrimaryKey(nameof(CycleNumber))]
public class Cycle
{
    public int CycleNumber { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    
    public ICollection<Suggestion> Suggestions { get; set; } = [];
    public ICollection<CycleVote> Votes { get; set; } = [];
}