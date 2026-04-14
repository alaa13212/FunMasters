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
    public DbSet<PendingNotification> PendingNotifications { get; set; }
    public DbSet<Badge> Badges { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<FunMasterComment> FunMasterComments { get; set; }
    
    
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

        builder.Entity<UserBadge>(entity =>
        {
            entity.HasOne(ub => ub.User)
                .WithMany(u => u.UserBadges)
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(ub => ub.Badge)
                .WithMany(b => b.UserBadges)
                .HasForeignKey(ub => ub.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.UserId, e.BadgeId })
                .IsUnique();
        });

        builder.Entity<FunMasterComment>(entity =>
        {
            entity.HasOne(c => c.TargetUser)
                .WithMany(u => u.ReceivedComments)
                .HasForeignKey(c => c.TargetUserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(c => c.Author)
                .WithMany(u => u.WrittenComments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Entity<Suggestion>().Property(e => e.ActiveAtUtc).HasConversion(utcConverter);
        builder.Entity<Suggestion>().Property(e => e.FinishedAtUtc).HasConversion(utcConverter);
        builder.Entity<SteamPlaytime>().Property(e => e.CapturedAtUtc).HasConversion(utcConverter);
        builder.Entity<SteamPlaytime>().Property(e => e.ForeverUpdatedAtUtc).HasConversion(utcConverter);
        builder.Entity<PendingNotification>().Property(e => e.SendAfterUtc).HasConversion(utcConverter);
        builder.Entity<PendingNotification>().Property(e => e.CreatedAtUtc).HasConversion(utcConverter);
        builder.Entity<Badge>().Property(e => e.CreatedAtUtc).HasConversion(utcConverter);
        builder.Entity<UserBadge>().Property(e => e.AssignedAtUtc).HasConversion(utcConverter);
        builder.Entity<FunMasterComment>().Property(e => e.CreatedAtUtc).HasConversion(utcConverter);
    }
    
}