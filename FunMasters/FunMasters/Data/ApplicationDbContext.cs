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
    public DbSet<SteamPlaytime> SteamPlaytimes { get; set; }
    
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<SteamPlaytime>(entity =>
        {
            entity.HasOne(sp => sp.User)
                .WithMany(u => u.SteamPlaytimes)
                .HasForeignKey(sp => sp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(sp => sp.Suggestion)
                .WithMany()
                .HasForeignKey(sp => sp.SuggestionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.SuggestionId, e.UserId })
                .IsUnique();
        });

        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Entity<Suggestion>().Property(e => e.ActiveAtUtc).HasConversion(utcConverter);
        builder.Entity<Suggestion>().Property(e => e.FinishedAtUtc).HasConversion(utcConverter);
        builder.Entity<SteamPlaytime>().Property(e => e.CapturedAtUtc).HasConversion(utcConverter);
        builder.Entity<SteamPlaytime>().Property(e => e.ForeverUpdatedAtUtc).HasConversion(utcConverter);
    }
    
}