using FunMasters.Data;
using FunMasters.Services;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Jobs;

public class WeeklyDigestJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WeeklyDigestJob> _logger;

    public WeeklyDigestJob(IServiceProvider services, ILogger<WeeklyDigestJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run every Sunday at 8 PM UTC+3 (5 PM UTC)
        var delay = GetDelayUntilNext(DayOfWeek.Sunday, 17);
        await Task.Delay(delay, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromDays(7));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendWeeklyDigestAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending weekly digest");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private static TimeSpan GetDelayUntilNext(DayOfWeek targetDay, int hourUtc)
    {
        var now = DateTime.UtcNow;
        var target = now.Date.AddHours(hourUtc);
        while (target.DayOfWeek != targetDay || target <= now)
            target = target.AddDays(1);
        return target - now;
    }

    private async Task SendWeeklyDigestAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lucian = scope.ServiceProvider.GetRequiredService<LucianGalade>();
        var playtimeService = scope.ServiceProvider.GetRequiredService<SteamPlaytimeService>();

        var now = FunMastersTime.UtcNow;
        var oneWeekAgo = now.AddDays(-7);

        // Check for a game that finished this week
        var finishedThisWeek = await db.Suggestions
            .Include(s => s.Ratings)
            .Include(s => s.SuggestedBy)
            .Where(s => s.Status == SuggestionStatus.Finished && s.FinishedAtUtc >= oneWeekAgo)
            .OrderByDescending(s => s.FinishedAtUtc)
            .FirstOrDefaultAsync(stoppingToken);

        string? finishedTitle = finishedThisWeek?.Title;
        string? finishedRating = null;
        string? finishedMostPlayed = null;

        if (finishedThisWeek != null)
        {
            if (finishedThisWeek.Ratings.Count > 0)
                finishedRating = $"{finishedThisWeek.AverageRating:F1}/10";

            var playtimes = await db.SteamPlaytimes
                .Include(sp => sp.User)
                .Where(sp => sp.SuggestionId == finishedThisWeek.Id && sp.PlaytimeForeverMinutes > 0)
                .OrderByDescending(sp => sp.PlaytimeForeverMinutes)
                .ToListAsync(stoppingToken);

            if (playtimes.Count > 0)
                finishedMostPlayed = playtimes[0].User?.UserName;
        }

        // Current active game
        var active = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .FirstOrDefaultAsync(s => s.Status == SuggestionStatus.Active, stoppingToken);

        string? activeTitle = active?.Title;
        int? activeDaysElapsed = null;
        int? activeDaysRemaining = null;
        List<(string UserName, int PlaytimeMinutes)>? activePlaytimes = null;

        if (active?.ActiveAtUtc != null && active.FinishedAtUtc != null)
        {
            var totalDays = (active.FinishedAtUtc.Value - active.ActiveAtUtc.Value).Days;
            activeDaysElapsed = (now - active.ActiveAtUtc.Value).Days;
            activeDaysRemaining = Math.Max(0, totalDays - activeDaysElapsed.Value);

            // Refresh playtimes from Steam before reporting
            try
            {
                await playtimeService.RefreshAllPlaytimesAsync(active.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh playtimes for weekly digest");
            }

            var playtimeRecords = await db.SteamPlaytimes
                .Include(sp => sp.User)
                .Where(sp => sp.SuggestionId == active.Id && sp.PlaytimeForeverMinutes > 0)
                .ToListAsync(stoppingToken);

            activePlaytimes = playtimeRecords
                .Select(sp => (sp.User?.UserName ?? "Unknown", sp.PlaytimeForeverMinutes!.Value))
                .ToList();
        }

        // Next game in queue
        var next = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .Where(s => s.Status == SuggestionStatus.Queued)
            .OrderBy(s => s.ActiveAtUtc)
            .FirstOrDefaultAsync(stoppingToken);

        // Pending ratings count
        var allFinished = await db.Suggestions
            .Include(s => s.Ratings)
            .Where(s => s.Status == SuggestionStatus.Finished)
            .ToListAsync(stoppingToken);

        var activeMemberIds = await db.Users
            .Where(u => u.CycleOrder > 0 && CouncilStatusRoles.ReceiveNotifications.Contains(u.CouncilStatus))
            .Select(u => u.Id)
            .ToListAsync(stoppingToken);

        var pendingRatings = 0;
        foreach (var fg in allFinished)
        {
            var raterIds = fg.Ratings.Select(r => r.RaterId).ToHashSet();
            pendingRatings += activeMemberIds.Count(id => !raterIds.Contains(id));
        }

        await lucian.SendWeeklyDigestAsync(
            finishedTitle, finishedRating, finishedMostPlayed,
            activeTitle, activeDaysElapsed, activeDaysRemaining, activePlaytimes,
            next?.Title, next?.ActiveAtUtc,
            pendingRatings);
    }
}