using FunMasters.Data;
using FunMasters.Services;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Jobs;

public class QuarterlyDigestJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<QuarterlyDigestJob> _logger;

    public QuarterlyDigestJob(IServiceProvider services, ILogger<QuarterlyDigestJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextQuarterStart();
            // Task.Delay doesn't accept values larger than int.MaxValue ms (~24.8 days)
            var maxDelay = TimeSpan.FromDays(1);
            while (delay > maxDelay && !stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(maxDelay, stoppingToken);
                delay = GetDelayUntilNextQuarterStart();
            }

            if (stoppingToken.IsCancellationRequested) break;

            await Task.Delay(delay, stoppingToken);

            try
            {
                await SendQuarterlyDigestAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending quarterly digest");
            }
        }
    }

    private static TimeSpan GetDelayUntilNextQuarterStart()
    {
        var now = DateTime.UtcNow;
        var target = NextQuarterStart(now);
        return target - now;
    }

    private static DateTime NextQuarterStart(DateTime now)
    {
        // Quarters start: Jan 1, Apr 1, Jul 1, Oct 1 at 5 PM UTC (8 PM UTC+3)
        int[] quarterMonths = [1, 4, 7, 10];
        var year = now.Year;

        foreach (var month in quarterMonths)
        {
            var candidate = new DateTime(year, month, 1, 17, 0, 0, DateTimeKind.Utc);
            if (candidate > now)
                return candidate;
        }

        // Next year's first quarter
        return new DateTime(year + 1, 1, 1, 17, 0, 0, DateTimeKind.Utc);
    }

    private async Task SendQuarterlyDigestAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lucian = scope.ServiceProvider.GetRequiredService<LucianGalade>();

        var ratedGames = await db.Suggestions
            .Include(s => s.Ratings)
            .Include(s => s.SuggestedBy)
            .Where(s => s.Status == SuggestionStatus.Finished && s.Ratings.Count > 0)
            .ToListAsync(stoppingToken);

        if (ratedGames.Count == 0) return;

        var topRated = ratedGames.OrderByDescending(g => g.AverageRating).First();
        var lowestRated = ratedGames.OrderBy(g => g.AverageRating).First();

        var allPlaytimes = await db.SteamPlaytimes
            .Include(sp => sp.Suggestion)
            .Where(sp => sp.PlaytimeForeverMinutes > 0 && sp.Suggestion != null)
            .GroupBy(sp => sp.SuggestionId)
            .Select(g => new { SuggestionId = g.Key, Total = g.Sum(sp => sp.PlaytimeForeverMinutes!.Value) })
            .ToListAsync(stoppingToken);

        var mostPlayedEntry = allPlaytimes.OrderByDescending(p => p.Total).FirstOrDefault();
        var mostPlayed = ratedGames.FirstOrDefault(g => g.Id == mostPlayedEntry?.SuggestionId) ?? topRated;

        var mostControversial = ratedGames
            .OrderByDescending(g => g.Ratings.Count > 1
                ? ComputeStdDev(g.Ratings.Select(r => (double)r.DecimalScore).ToList())
                : 0)
            .First();

        string? mostDivisiveTitle = null;
        if (ratedGames.Count > 2)
        {
            var divisive = ratedGames
                .Where(g => g.Ratings.Count > 1)
                .OrderByDescending(g =>
                {
                    var scores = g.Ratings.Select(r => r.DecimalScore).ToList();
                    var hasHigh = scores.Any(s => s >= 7);
                    var hasLow = scores.Any(s => s <= 4);
                    return hasHigh && hasLow ? 1 : 0;
                })
                .FirstOrDefault();

            if (divisive != null && divisive.Ratings.Any(r => r.DecimalScore >= 7) &&
                divisive.Ratings.Any(r => r.DecimalScore <= 4))
                mostDivisiveTitle = divisive.Title;
        }

        await lucian.SendQuarterlyDigestAsync(
            topRated.Title, topRated.AverageRating ?? 0,
            lowestRated.Title, lowestRated.AverageRating ?? 0,
            mostPlayed.Title, mostPlayedEntry?.Total ?? 0,
            mostControversial.Title,
            mostDivisiveTitle);
    }

    private static double ComputeStdDev(List<double> values)
    {
        if (values.Count < 2) return 0;
        var avg = values.Average();
        var sumSq = values.Sum(v => (v - avg) * (v - avg));
        return Math.Sqrt(sumSq / values.Count);
    }
}