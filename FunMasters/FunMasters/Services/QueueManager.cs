using FunMasters.Data;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

public class QueueManager(ApplicationDbContext db)
{
    private static readonly TimeSpan GamePlayPeriod = TimeSpan.FromDays(14);
    
    public async Task UpdateQueueAsync()
    {
        var now = DateTime.UtcNow;
        var active = await db.Suggestions
            .Include(s => s.SuggestedBy)
            .FirstOrDefaultAsync(s => s.Status == SuggestionStatus.Active);

        // Check if active game expired
        if (active is { FinishedAtUtc: not null } && now > active.FinishedAtUtc)
        {
            active.Status = SuggestionStatus.Finished;
        }

        // If there’s no active game, promote next one
        if (!await db.Suggestions.AnyAsync(s => s.Status == SuggestionStatus.Active))
        {
            var next = await GetNextSuggestionAsync();
            if (next != null)
            {
                next.Status = SuggestionStatus.Active;
            }
        }
        await db.SaveChangesAsync();

        // Fill queue if needed
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
        DateTime referenceTime = DateTime.UtcNow.Date;

        if (lastGame?.SuggestedBy != null)
        {
            cycleOrder = lastGame.SuggestedBy.CycleOrder + 1;
            referenceTime = lastGame.FinishedAtUtc ?? DateTime.UtcNow.Date;
        }

        var orderedUsers = users
            .OrderBy(u => u.CycleOrder >= cycleOrder ? -1 : 1)
            .ThenBy(u => u.CycleOrder)
            .ToList();
        
        // For each user that doesn't have one queued, pick their earliest pending
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
            referenceTime += GamePlayPeriod;
            pending.FinishedAtUtc = referenceTime;
                
            if(pending.ActiveAtUtc < DateTime.UtcNow && pending.FinishedAtUtc > DateTime.UtcNow)
                pending.Status = SuggestionStatus.Active;
            
        }
        
        await db.SaveChangesAsync();
    }
}