using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FunMasters.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    
    public DbSet<Suggestion> Suggestions { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<Cycle> Cycles { get; set; }
    public DbSet<CycleVote> CycleVotes { get; set; }
    
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Entity<Suggestion>().Property(e => e.ActiveAtUtc).HasConversion(utcConverter);
        builder.Entity<Suggestion>().Property(e => e.FinishedAtUtc).HasConversion(utcConverter);
    }
    
}