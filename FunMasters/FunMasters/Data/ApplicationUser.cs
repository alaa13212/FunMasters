using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FunMasters.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public bool RequirePasswordChange { get; set; } = true;
    public int CycleOrder { get; set; }
    
    public DateTime RegistrationDateUtc { get; set; } = DateTime.UtcNow;
    
    [StringLength(17)]
    public string? SteamId { get; set; }
    
    public ICollection<SteamPlaytime> SteamPlaytimes { get; set; } = [];
    
    public ICollection<Suggestion> Suggestions { get; set; } = [];
    
    
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
        SecurityStamp = Guid.NewGuid().ToString();
    }
    
}