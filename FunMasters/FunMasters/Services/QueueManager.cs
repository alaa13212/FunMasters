using FunMasters.Data;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

public class QueueManager(
    ApplicationDbContext db,
    SteamPlaytimeService steamPlaytimeService,
    LucianGalade lucianGalade,
    CycleService cycleService)
{
    public async Task UpdateQueueAsync()
    {
        var now = FunMastersTime.UtcNow;
        var active = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .FirstOrDefaultAsync(s => s.Status == SuggestionStatus.Active);

        Suggestion? finished = null;
        if (active is { FinishedAtUtc: not null } && now >= active.FinishedAtUtc.Value)
        {
            finished = active;
            active.Status = SuggestionStatus.Finished;
            await db.SaveChangesAsync();
            await steamPlaytimeService.CaptureAllPlaytimesOnFinishAsync(active.Id);
        }

        if (!await db.Suggestions.AnyAsync(s => s.Status == SuggestionStatus.Active))
        {
            Suggestion? next = await GetNextSuggestionAsync();
            if (next != null)
            {
                next.Status = SuggestionStatus.Active;
                await cycleService.StampOnPromotionAsync(next, finished);
                if (finished != null)
                    await NotifyGameRotationAsync(finished, next);
            }
        }
        await db.SaveChangesAsync();

        if (finished != null)
            await AnnounceDeferredLongReviewsAsync(finished);

        await RebuildQueueAsync();

        await cycleService.StampMissingAsync();
        await cycleService.SettleDueCyclesAsync();
    }

    private async Task NotifyGameRotationAsync(Suggestion outgoing, Suggestion incoming)
    {
        await db.Entry(incoming).Reference(s => s.SuggestedBy).LoadAsync();

        var playtimes = await db.SteamPlaytimes
            .Include(sp => sp.User)
            .Where(sp => sp.SuggestionId == outgoing.Id && sp.Playtime2WeeksMinutes > 0)
            .ToListAsync();

        var playtimeData = playtimes.Select(sp => (sp.User!.UserName ?? "Unknown", sp.Playtime2WeeksMinutes!.Value));

        var message = LucianGalade.BuildGameRotationMessage(
            outgoing.Title, playtimeData, incoming.Title,
            incoming.SuggestedBy?.UserName ?? "Unknown", incoming.SteamLink);

        await lucianGalade.QueueForMorningAsync(LucianTriggers.GameRotation, message);
    }

    /// <summary>
    /// Long reviews delivered while the title was still in deliberation were held back, since gems
    /// only open once it concludes. They go out now, queued behind the rotation message so the
    /// morning reads in order.
    /// </summary>
    private async Task AnnounceDeferredLongReviewsAsync(Suggestion finished)
    {
        var unannounced = await db.Ratings
            .Include(r => r.Rater)
            .Where(r => r.SuggestionId == finished.Id && r.LongReviewAnnouncedAtUtc == null)
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync();

        foreach (var rating in unannounced.Where(r => ReviewRules.IsLong(r.Comment)))
            await lucianGalade.AnnounceLongReviewAsync(
                rating, rating.Rater?.UserName ?? "Unknown", finished.Title, holdForMorning: true);
    }
    
    public async Task<Suggestion?> GetNextSuggestionAsync()
    {
        return await db.Suggestions
            .Where(s => s.Status == SuggestionStatus.Queued)
            .OrderBy(s => s.ActiveAtUtc)
            .FirstOrDefaultAsync();
    }
    
    public async Task RebuildQueueAsync()
    {
        // Only active members participate in the queue
        var users = await db.Users
            .Where(u => u.CycleOrder > 0 && CouncilStatusRoles.InQueue.Contains(u.CouncilStatus))
            .ToListAsync();

        var lastGame = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .Where(s => s.Status == SuggestionStatus.Finished || s.Status == SuggestionStatus.Active)
            .OrderByDescending(s => s.FinishedAtUtc)
            .FirstOrDefaultAsync();

        int cycleOrder = 1;
        DateTime referenceTime = FunMastersTime.CurrentOrNextMidnightUtc3();

        if (lastGame?.SuggestedBy != null)
        {
            cycleOrder = lastGame.SuggestedBy.CycleOrder + 1;
            referenceTime = lastGame.FinishedAtUtc ?? FunMastersTime.CurrentOrNextMidnightUtc3();
        }

        var orderedUsers = users
            .OrderBy(u => u.CycleOrder >= cycleOrder ? -1 : 1)
            .ThenBy(u => u.CycleOrder)
            .ToList();
        
        foreach (var user in orderedUsers)
        {
            var pending = await db.Suggestions
                .Where(s => s.SuggestedById == user.Id && s.Status != SuggestionStatus.Finished)
                .OrderBy(s => s.Order)
                .FirstOrDefaultAsync();

            if (pending == null)
                continue;
            
            if(pending.Status == SuggestionStatus.Active)
                continue;
                
            pending.Status = SuggestionStatus.Queued;
            pending.ActiveAtUtc = referenceTime;
            referenceTime += FunMastersTime.GamePlayPeriod;
            pending.FinishedAtUtc = referenceTime;
                
            if(pending.ActiveAtUtc < FunMastersTime.UtcNow && pending.FinishedAtUtc > FunMastersTime.UtcNow)
                pending.Status = SuggestionStatus.Active;
        }
        
        await db.SaveChangesAsync();
    }
}
