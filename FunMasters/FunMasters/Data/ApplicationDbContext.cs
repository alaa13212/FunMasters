using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    
    public DbSet<Suggestion> Suggestions { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<Cycle> Cycles { get; set; }
    public DbSet<CycleVote> CycleVotes { get; set; }
    
}