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
    
    public string? Bio { get; set; }
    public CouncilStatus CouncilStatus { get; set; } = CouncilStatus.Active;
    
    public ICollection<Suggestion> Suggestions { get; set; } = [];
    public ICollection<UserBadge> UserBadges { get; set; } = [];
    public ICollection<FunMasterComment> ReceivedComments { get; set; } = [];
    public ICollection<FunMasterComment> WrittenComments { get; set; } = [];
    
    
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
        SecurityStamp = Guid.NewGuid().ToString();
    }
    
}