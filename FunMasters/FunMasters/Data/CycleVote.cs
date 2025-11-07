using Microsoft.EntityFrameworkCore;

namespace FunMasters.Data;

[Index(nameof(CycleNumber), nameof(VoterId), nameof(VotedGameId), IsUnique = true)]
public class CycleVote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public int CycleNumber { get; set; }
    public Cycle? Cycle { get; set; }
    
    public Guid VoterId { get; set; }
    public ApplicationUser? Voter { get; set; }
    
    public Guid VotedGameId { get; set; }
    public Suggestion? VotedGame { get; set; }
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}