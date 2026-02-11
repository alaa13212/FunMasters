using System.Security.Claims;

namespace FunMasters.Shared.DTOs;

public class LoggedInUserInfo
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string AvatarTimestamp { get; set; }
    public List<string> Roles { get; set; }
}