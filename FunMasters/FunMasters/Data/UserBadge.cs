namespace FunMasters.Data;

public sealed class UserBadge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid BadgeId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    
    public ApplicationUser User { get; set; } = null!;
    public Badge Badge { get; set; } = null!;
}
