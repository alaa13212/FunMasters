namespace FunMasters.Shared.DTOs;

public class LoginResponse
{
    public bool Succeeded { get; set; }
    public bool RequirePasswordChange { get; set; }
    public bool IsLockedOut { get; set; }
    public string? ErrorMessage { get; set; }
}
