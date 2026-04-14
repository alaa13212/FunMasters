using FunMasters.Data;
using FunMasters.Services;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Jobs;

public class PlaytimeMilestoneJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PlaytimeMilestoneJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly int[] MilestoneMinutes = [300, 600, 1200, 1800];

    public PlaytimeMilestoneJob(IServiceProvider services, ILogger<PlaytimeMilestoneJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckMilestonesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking playtime milestones");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CheckMilestonesAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lucian = scope.ServiceProvider.GetRequiredService<LucianGalade>();
        var playtimeService = scope.ServiceProvider.GetRequiredService<SteamPlaytimeService>();

        var active = await db.Suggestions
            .FirstOrDefaultAsync(s => s.Status == SuggestionStatus.Active, stoppingToken);

        if (active == null) return;

        var appId = SteamPlaytimeService.ExtractSteamAppId(active.SteamLink);
        if (appId == null) return;

        var members = await db.Users
            .Where(u => u.CycleOrder > 0 && CouncilStatusRoles.ReceiveNotifications.Contains(u.CouncilStatus) && u.SteamId != null)
            .ToListAsync(stoppingToken);

        foreach (var member in members)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var existing = await db.SteamPlaytimes
                .FirstOrDefaultAsync(sp => sp.SuggestionId == active.Id && sp.UserId == member.Id,
                    stoppingToken);

            var previousMinutes = existing?.Playtime2WeeksMinutes ?? 0;

            int currentMinutes;
            int foreverMinutes;
            try
            {
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                var result = await steamService.GetPlaytimeAsync(member.SteamId!, appId.Value);
                currentMinutes = result?.playtime2Weeks ?? 0;
                foreverMinutes = result?.playtimeForever ?? 0;
            }
            catch
            {
                continue;
            }

            if (currentMinutes <= previousMinutes) continue;

            // Find which milestone was crossed
            var crossedMilestone = MilestoneMinutes
                .LastOrDefault(m => previousMinutes < m && currentMinutes >= m);

            if (crossedMilestone > 0)
            {
                var milestoneLabel = $"{crossedMilestone / 60} Hours";

                await lucian.SendPlaytimeMilestoneAsync(
                    active.Title, member.UserName ?? "Unknown", milestoneLabel, currentMinutes);
            }

            // Update stored playtime
            if (existing == null)
            {
                db.SteamPlaytimes.Add(new SteamPlaytime
                {
                    SuggestionId = active.Id,
                    UserId = member.Id,
                    Playtime2WeeksMinutes = currentMinutes,
                    PlaytimeForeverMinutes = foreverMinutes,
                    ForeverUpdatedAtUtc = DateTime.UtcNow,
                    CapturedAtUtc = DateTime.UtcNow
                });
            }
            else if (currentMinutes > (existing.Playtime2WeeksMinutes ?? 0))
            {
                existing.Playtime2WeeksMinutes = currentMinutes;
                existing.PlaytimeForeverMinutes = foreverMinutes;
                existing.ForeverUpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }
}