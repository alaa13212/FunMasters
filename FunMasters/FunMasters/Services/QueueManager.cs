using FunMasters.Data;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

public class QueueManager(ApplicationDbContext db, SteamPlaytimeService steamPlaytimeService, LucianGalade lucianGalade)
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
                if (finished != null)
                    await NotifyGameRotationAsync(finished, next);
            }
        }
        await db.SaveChangesAsync();

        await RebuildQueueAsync();
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

        await lucianGalade.QueueForMorningAsync(message);
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
        var users = await db.Users.Where(u => u.CycleOrder > 0).ToListAsync();

        var lastGame = await db.Suggestions
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
