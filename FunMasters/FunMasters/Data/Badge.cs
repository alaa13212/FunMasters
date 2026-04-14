namespace FunMasters.Data;

public sealed class Badge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    public ICollection<UserBadge> UserBadges { get; set; } = [];
}
