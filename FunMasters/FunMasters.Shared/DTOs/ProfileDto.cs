namespace FunMasters.Shared.DTOs;

public class ProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
}
