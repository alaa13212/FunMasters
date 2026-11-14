using FunMasters.Data;
using FunMasters.Services;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Jobs;

public class RatingReminderJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RatingReminderJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public RatingReminderJob(IServiceProvider services, ILogger<RatingReminderJob> logger)
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
        var delay = target - now;
        await Task.Delay(delay, stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckRatingRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rating reminders");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task CheckRatingRemindersAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lucian = scope.ServiceProvider.GetRequiredService<LucianGalade>();

        DateTime now = FunMastersTime.UtcNow;
        DateTime threeDaysAgo = now.AddDays(-3);
        DateTime tooOld = now - CycleService.SettlementPeriod;

        // The window is narrower than a deliberation period, so only the title that concluded most
        // recently is ever chased — older ones fall out of it on their own.
        var finishedGames = await db.Suggestions
            .Include(s => s.Ratings)
            .ThenInclude(r => r.Rater)
            .Include(s => s.SuggestedBy)
            .Where(s => s.Status == SuggestionStatus.Finished && s.FinishedAtUtc <= threeDaysAgo && s.FinishedAtUtc >= tooOld)
            .ToListAsync(stoppingToken);

        var allActiveMembers = await db.Users
            .Where(u => u.CycleOrder > 0 && CouncilStatusRoles.MustReview.Contains(u.CouncilStatus))
            .ToListAsync(stoppingToken);

        foreach (var game in finishedGames)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var daysSinceFinish = (int)(now - game.FinishedAtUtc!.Value).TotalDays;

            // Only remind at day 3, 5, and then every 5 days after
            if (daysSinceFinish != 3 && daysSinceFinish != 5 && daysSinceFinish % 5 != 0)
                continue;

            // Exclude members who registered after the game was active
            var cutoff = OffenceRules.Cutoff(game);
            var eligibleMembers = allActiveMembers
                .Where(u => OffenceRules.OwesVerdict(u, cutoff))
                .ToList();
            var eligibleIds = eligibleMembers.Select(u => u.Id).ToHashSet();

            var raterIds = game.Ratings.Select(r => r.RaterId).ToHashSet();
            var unratedMembers = eligibleIds.Where(id => !raterIds.Contains(id)).ToList();

            // Two days before the Writer of the title is named, one message chases both verdicts and
            // gems, and takes the place of that day's rating reminder.
            if (daysSinceFinish == LucianTriggers.GemReminderDays && await lucian.IsEnabledAsync(LucianTriggers.GemReminder))
            {
                var unratedNames = unratedMembers
                    .Select(id => eligibleMembers.First(u => u.Id == id).UserName ?? "Unknown")
                    .ToList();

                var awardedBy = (await db.Gems
                    .Where(g => g.SuggestionId == game.Id)
                    .Select(g => g.AwardedById)
                    .ToListAsync(stoppingToken)).ToHashSet();
                var unspentNames = GemRules.UnspentGemHolders(game.Ratings, awardedBy)
                    .Select(r => r.Rater?.UserName ?? "Unknown")
                    .OrderBy(n => n)
                    .ToList();

                if (unratedNames.Count > 0 || unspentNames.Count > 0)
                    await lucian.SendGemReminderAsync(
                        game.Title, daysSinceFinish, LucianTriggers.TitleWriterDays - daysSinceFinish,
                        unratedNames, unspentNames, isRepeat: daysSinceFinish > 3);

                if (unratedMembers.Count > 0)
                    continue;
            }
            else if (unratedMembers.Count > 0)
            {
                var memberNames = new List<string>();
                foreach (var memberId in unratedMembers)
                {
                    var user = eligibleMembers.First(u => u.Id == memberId);
                    memberNames.Add(user.UserName ?? "Unknown");
                }

                await lucian.SendRatingReminderAsync(
                    game.Title, unratedMembers.Count, daysSinceFinish, memberNames,
                    includeShortCommentMembers: false, isRepeat: daysSinceFinish > 3);
                continue;
            }

            // Check for short/missing comments
            var shortCommentRaters = game.Ratings
                .Where(r => eligibleIds.Contains(r.RaterId) && !OffenceRules.IsVerdictSubstantial(r.Comment))
                .ToList();

            if (shortCommentRaters.Count > 0)
            {
                var names = shortCommentRaters.Select(r => r.Rater?.UserName ?? "Unknown");
                await lucian.SendRatingReminderAsync(
                    game.Title, shortCommentRaters.Count, daysSinceFinish, names,
                    includeShortCommentMembers: true, isRepeat: daysSinceFinish > 3);
            }
        }
    }

}