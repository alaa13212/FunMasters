namespace FunMasters.Shared.DTOs;

public class BadgeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}

public class UserBadgeDto
{
    public Guid BadgeId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}
