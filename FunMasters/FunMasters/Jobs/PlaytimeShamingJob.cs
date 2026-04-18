using FunMasters.Data;
using FunMasters.Services;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Jobs;

public class PlaytimeShamingJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PlaytimeShamingJob> _logger;

    public PlaytimeShamingJob(IServiceProvider services, ILogger<PlaytimeShamingJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Align to 9 AM UTC+3 (6 AM UTC)
        var now = DateTime.UtcNow;
        var target = now.Date.AddHours(6);
        if (now >= target) target = target.AddDays(1);
        await Task.Delay(target - now, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckShamingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in playtime shaming");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task CheckShamingAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lucian = scope.ServiceProvider.GetRequiredService<LucianGalade>();
        var playtimeService = scope.ServiceProvider.GetRequiredService<SteamPlaytimeService>();

        var now = FunMastersTime.UtcNow;

        var active = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .FirstOrDefaultAsync(s => s.Status == SuggestionStatus.Active, stoppingToken);

        if (active == null || active.ActiveAtUtc == null || active.FinishedAtUtc == null) return;

        var totalDays = (active.FinishedAtUtc.Value - active.ActiveAtUtc.Value).Days;
        var daysElapsed = (now - active.ActiveAtUtc.Value).Days;
        var daysRemaining = totalDays - daysElapsed;

        // Only trigger at midpoint and at 2 days remaining
        var isMidpoint = daysElapsed == (totalDays / 2 - 2);
        var isTwoDaysLeft = daysRemaining == 2;

        if (!isMidpoint && !isTwoDaysLeft) return;

        // Refresh playtimes from Steam before checking
        try
        {
            await playtimeService.RefreshAllPlaytimesAsync(active.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh playtimes for shaming check");
        }

        var appId = SteamPlaytimeService.ExtractSteamAppId(active.SteamLink);
        if (appId == null) return;

        var members = await db.Users
            .Where(u => u.CycleOrder > 0 && CouncilStatusRoles.ShamingNotifications.Contains(u.CouncilStatus) && u.SteamId != null)
            .ToListAsync(stoppingToken);

        var absentMembers = new List<string>();

        foreach (var member in members)
        {
            var playtimes = await db.SteamPlaytimes
                .Where(sp => sp.SuggestionId == active.Id && sp.UserId == member.Id)
                .FirstOrDefaultAsync(stoppingToken);

            var hasPlayed = playtimes?.PlaytimeForeverMinutes > 0;

            if (!hasPlayed)
            {
                // Double-check via Steam API
                try
                {
                    var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                    var result = await steamService.GetPlaytimeAsync(member.SteamId!, appId.Value);
                    if (result == null || result.Value.playtimeForever == 0)
                        absentMembers.Add(member.UserName ?? "Unknown");
                }
                catch
                {
                    absentMembers.Add(member.UserName ?? "Unknown");
                }
            }
        }

        if (absentMembers.Count > 0)
        {
            await lucian.SendPlaytimeShamingAsync(active.Title, daysRemaining, absentMembers, isRepeat: isTwoDaysLeft);
        }
    }
}