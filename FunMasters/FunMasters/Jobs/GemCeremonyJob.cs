using FunMasters.Data;
using FunMasters.Services;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Jobs;

/// <summary>
/// Every morning at 9 AM (UTC+3): Lucian reads out the gems awarded the day before, and names the
/// Writer of every title whose gems are due to be counted.
/// </summary>
public class GemCeremonyJob(IServiceProvider services, ILogger<GemCeremonyJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    /// <summary>
    /// A title whose naming day passed longer ago than this is left unnamed: it concluded before
    /// titles had Writers, or the server was down through its day. Naming it late would be stale news.
    /// </summary>
    private static readonly TimeSpan NamingGrace = TimeSpan.FromDays(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Align to 9 AM UTC+3 (6 AM UTC), like the other morning jobs.
        var now = DateTime.UtcNow;
        var target = now.Date.AddHours(6);
        if (now >= target) target = target.AddDays(1);
        await Task.Delay(target - now, stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running the gem ceremony");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lucian = scope.ServiceProvider.GetRequiredService<LucianGalade>();

        await AnnounceYesterdaysGemsAsync(db, lucian, stoppingToken);
        await NameTitleWritersAsync(db, lucian, stoppingToken);
    }

    /// <summary>Gems awarded on the previous calendar day in UTC+3. Only receivers are named.</summary>
    private static async Task AnnounceYesterdaysGemsAsync(ApplicationDbContext db, LucianGalade lucian, CancellationToken ct)
    {
        var localToday = (FunMastersTime.UtcNow + FunMastersTime.UtcPlus3).Date;
        var todayStartUtc = DateTime.SpecifyKind(localToday - FunMastersTime.UtcPlus3, DateTimeKind.Utc);
        var yesterdayStartUtc = todayStartUtc.AddDays(-1);

        var gems = await db.Gems
            .Where(g => g.AwardedAtUtc >= yesterdayStartUtc && g.AwardedAtUtc < todayStartUtc)
            .OrderBy(g => g.AwardedAtUtc)
            .Select(g => new { Receiver = g.Rating!.Rater!.UserName, g.Suggestion!.Title })
            .ToListAsync(ct);

        await lucian.SendGemRoundupAsync(gems.Select(g => (g.Receiver ?? "Unknown", g.Title)).ToList());
    }

    /// <summary>
    /// The Writer of a title is the member whose review on it drew the most gems; ties fall to whoever
    /// drew their first gem earliest, as for the Writer of the Cycle. A title is stamped even when no
    /// gem was given, so it is never counted twice, but Lucian only speaks when there is a Writer.
    /// </summary>
    private static async Task NameTitleWritersAsync(ApplicationDbContext db, LucianGalade lucian, CancellationToken ct)
    {
        var now = FunMastersTime.UtcNow;
        var dueBy = now.AddDays(-LucianTriggers.TitleWriterDays);
        var tooOld = dueBy - NamingGrace;

        var due = await db.Suggestions
            .Where(s => s.Status == SuggestionStatus.Finished
                        && s.TitleWriterNamedAtUtc == null
                        && s.FinishedAtUtc != null
                        && s.FinishedAtUtc <= dueBy
                        && s.FinishedAtUtc > tooOld)
            .ToListAsync(ct);

        foreach (var title in due)
        {
            var standings = await db.Gems
                .Where(g => g.SuggestionId == title.Id)
                .GroupBy(g => g.Rating!.RaterId)
                .Select(x => new { UserId = x.Key, Count = x.Count(), FirstAwardedAtUtc = x.Min(g => g.AwardedAtUtc) })
                .ToListAsync(ct);

            var winner = standings
                .OrderByDescending(s => s.Count)
                .ThenBy(s => s.FirstAwardedAtUtc)
                .FirstOrDefault();

            title.TitleWriterUserId = winner?.UserId;
            title.TitleWriterGemCount = winner?.Count;
            title.TitleWriterNamedAtUtc = now;
            await db.SaveChangesAsync(ct);

            if (winner == null)
                continue;

            var writerName = await db.Users
                .Where(u => u.Id == winner.UserId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync(ct) ?? "Unknown";

            await lucian.SendTitleWriterAsync(title.Id, title.Title, writerName, winner.Count);
        }
    }
}
