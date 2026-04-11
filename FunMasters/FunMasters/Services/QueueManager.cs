using FunMasters.Data;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

public class QueueManager(ApplicationDbContext db, SteamPlaytimeService steamPlaytimeService)
{
    public async Task UpdateQueueAsync()
    {
        var now = FunMastersTime.UtcNow;
        var active = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .FirstOrDefaultAsync(s => s.Status == SuggestionStatus.Active);

        if (active is { FinishedAtUtc: not null } && now >= active.FinishedAtUtc.Value)
        {
            active.Status = SuggestionStatus.Finished;
            await db.SaveChangesAsync();
            await steamPlaytimeService.CaptureAllPlaytimesOnFinishAsync(active.Id);
        }

        if (!await db.Suggestions.AnyAsync(s => s.Status == SuggestionStatus.Active))
        {
            Suggestion? next = await GetNextSuggestionAsync();
            next?.Status = SuggestionStatus.Active;
        }
        await db.SaveChangesAsync();

        await RebuildQueueAsync();
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
