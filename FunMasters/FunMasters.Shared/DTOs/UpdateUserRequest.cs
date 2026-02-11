namespace FunMasters.Shared.DTOs;

public class UpdateUserRequest
{
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public int CycleOrder { get; set; }
    public bool IsAdmin { get; set; }
}
