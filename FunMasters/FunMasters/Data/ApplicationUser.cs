using Microsoft.AspNetCore.Identity;

namespace FunMasters.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public bool RequirePasswordChange { get; set; } = true;
    public int CycleOrder { get; set; }
    
    public ICollection<Suggestion> Suggestions { get; set; } = [];
    
    
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
        SecurityStamp = Guid.NewGuid().ToString();
    }
    
}