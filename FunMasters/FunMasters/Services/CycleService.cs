using FunMasters.Data;
using FunMasters.Shared;
using FunMasters.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

/// <summary>
/// Keeps the Council's rotations on the record.
///
/// A cycle is one full rotation: the longest run of titles in which no member appears twice.
/// A new cycle opens at the first promotion whose suggester already holds a title in the
/// current one. That test reads only stamped history, so it survives an admin renumbering
/// <see cref="ApplicationUser.CycleOrder"/> mid-rotation or a member being cast out afterwards —
/// both of which would fool a rule that merely compared the incoming and outgoing orders.
/// </summary>
public class CycleService(ApplicationDbContext db, LucianGalade lucianGalade, ILogger<CycleService> logger)
{
    /// <summary>
    /// How long after a cycle closes before its Writer is settled. A cycle closes the instant
    /// its final title concludes — before anyone has delivered a verdict on that title, let alone
    /// gemmed one. Settling immediately would disenfranchise the last title of every rotation.
    /// Matches the horizon over which <see cref="Jobs.RatingReminderJob"/> chases verdicts.
    /// </summary>
    public static readonly TimeSpan SettlementPeriod = TimeSpan.FromDays(14);

    /// <summary>
    /// Partitions the Council's existing history into cycles. One-shot: it walks from zero, so it
    /// only runs when no cycle exists at all. Titles that appear unstamped later are picked up by
    /// <see cref="StampMissingAsync"/> instead, which appends rather than re-partitioning.
    /// (Re-running this means clearing Suggestions.CycleNumber first — the foreign key blocks
    /// deleting the Cycles rows on their own.)
    /// </summary>
    public async Task EnsureCyclesBackfilledAsync()
    {
        if (await db.Cycles.AnyAsync())
            return;

        var games = await StampableGamesQuery().ToListAsync();
        if (games.Count == 0)
            return;

        Cycle? current = null;
        Suggestion? previous = null;
        var seen = new HashSet<Guid>();
        var cycleCount = 0;

        foreach (var game in games)
        {
            if (current == null || seen.Contains(game.SuggestedById))
            {
                if (current != null)
                {
                    current.EndAtUtc = previous?.FinishedAtUtc;
                    seen.Clear();
                }

                current = new Cycle { StartAtUtc = StartOf(game) };
                db.Cycles.Add(current);
                cycleCount++;
            }

            seen.Add(game.SuggestedById);
            game.Cycle = current;
            previous = game;
        }

        // The final cycle stays open — it closes when the next promotion wraps the rotation.
        await db.SaveChangesAsync();
        logger.LogInformation(
            "Backfilled {GameCount} title(s) into {CycleCount} cycle(s).", games.Count, cycleCount);
    }

    /// <summary>
    /// Stamps a title with its cycle as it goes before the Council, opening a new cycle when the
    /// rotation has wrapped. Assigns the navigation property rather than the number so EF inserts
    /// the Cycle row before the foreign key that points at it. The caller saves.
    /// </summary>
    public async Task StampOnPromotionAsync(Suggestion incoming, Suggestion? outgoing)
    {
        var current = await db.Cycles
            .OrderByDescending(c => c.CycleNumber)
            .FirstOrDefaultAsync();

        var wrapped = current != null && await db.Suggestions.AnyAsync(s =>
            s.CycleNumber == current.CycleNumber && s.SuggestedById == incoming.SuggestedById);

        if (current == null || wrapped)
        {
            if (current != null)
                // A rotation ends when its last title concluded. Falling back to the clock
                // would close an in-progress cycle at "now" whenever a back-dated title is
                // stamped late by StampMissingAsync.
                current.EndAtUtc = outgoing?.FinishedAtUtc
                                   ?? await LastFinishAsync(current.CycleNumber)
                                   ?? FunMastersTime.UtcNow;

            current = new Cycle { StartAtUtc = incoming.ActiveAtUtc ?? FunMastersTime.UtcNow };
            db.Cycles.Add(current);
        }

        incoming.Cycle = current;
    }

    /// <summary>
    /// Catches titles that reached the Council without passing through a promotion — an admin
    /// activating one by hand, say. Normally finds nothing.
    /// </summary>
    public async Task StampMissingAsync()
    {
        var unstamped = await StampableGamesQuery()
            .Where(s => s.CycleNumber == null)
            .ToListAsync();

        foreach (var game in unstamped)
        {
            // Saved one at a time: the wrap test reads committed history.
            await StampOnPromotionAsync(game, null);
            await db.SaveChangesAsync();
        }

        if (unstamped.Count > 0)
            logger.LogInformation("Stamped {Count} previously unstamped title(s).", unstamped.Count);
    }

    /// <summary>
    /// Names the Writer of the Cycle for every closed cycle whose settlement period has elapsed.
    /// Gems count toward the cycle their title was stamped with, not the moment they were given,
    /// so standings do not depend on when members happened to click.
    /// </summary>
    public async Task SettleDueCyclesAsync()
    {
        var cutoff = FunMastersTime.UtcNow - SettlementPeriod;

        var due = await db.Cycles
            .Where(c => c.EndAtUtc != null && c.WriterSettledAtUtc == null && c.EndAtUtc < cutoff)
            .ToListAsync();

        if (due.Count == 0)
            return;

        var named = new List<(int CycleNumber, Guid UserId, int GemCount)>();
        foreach (var cycle in due)
        {
            var writer = await SettleAsync(cycle);
            if (writer is { } w)
                named.Add((cycle.CycleNumber, w.UserId, w.GemCount));
        }

        await db.SaveChangesAsync();

        // Announced only from this sweep: an admin's recompute corrects the record quietly.
        foreach (var (cycleNumber, userId, gemCount) in named)
        {
            var writerName = await db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync() ?? "Unknown";
            await lucianGalade.SendCycleWriterAsync(cycleNumber, writerName, gemCount);
        }
    }

    /// <summary>
    /// Names a cycle's Writer again from the gems standing right now. Gems may be awarded on old
    /// reviews indefinitely, so a settled result can fall out of date; this is the deliberate
    /// correction for that, rather than letting a title change hands silently.
    /// </summary>
    public async Task<ApiResult> RecomputeWriterAsync(int cycleNumber)
    {
        var cycle = await db.Cycles.FirstOrDefaultAsync(c => c.CycleNumber == cycleNumber);
        if (cycle == null)
            return ApiResult.Fail($"Cycle {cycleNumber} does not exist");

        if (cycle.EndAtUtc == null)
            return ApiResult.Fail("This cycle is still in progress; there is nothing to settle yet");

        await SettleAsync(cycle);
        await db.SaveChangesAsync();
        return ApiResult.Ok();
    }

    /// <returns>The Writer and their gem count, or null when no gem was given in the cycle.</returns>
    private async Task<(Guid UserId, int GemCount)?> SettleAsync(Cycle cycle)
    {
        var standings = await db.Gems
            .Where(g => g.Suggestion!.CycleNumber == cycle.CycleNumber)
            .GroupBy(g => g.Rating!.RaterId)
            .Select(x => new
            {
                UserId = x.Key,
                Count = x.Count(),
                FirstAwardedAtUtc = x.Min(g => g.AwardedAtUtc)
            })
            .ToListAsync();

        // Ties fall to whoever drew their first gem earliest.
        var winner = standings
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.FirstAwardedAtUtc)
            .FirstOrDefault();

        cycle.WriterOfTheCycleUserId = winner?.UserId;
        cycle.WriterGemCount = winner?.Count;
        // Stamped even when no gems were given, so the sweep does not revisit this cycle.
        cycle.WriterSettledAtUtc = FunMastersTime.UtcNow;

        logger.LogInformation(
            "Cycle {CycleNumber} settled; Writer of the Cycle: {Winner}.",
            cycle.CycleNumber, winner?.UserId.ToString() ?? "none (no gems awarded)");

        return winner == null ? null : (winner.UserId, winner.Count);
    }

    /// When the last title of a cycle concluded — used to close it at the right moment
    /// rather than at wall-clock time.
    private async Task<DateTime?> LastFinishAsync(int cycleNumber) =>
        await db.Suggestions
            .Where(s => s.CycleNumber == cycleNumber && s.FinishedAtUtc != null)
            .MaxAsync(s => (DateTime?)s.FinishedAtUtc);

    private IQueryable<Suggestion> StampableGamesQuery() =>
        db.Suggestions
            .Where(s => s.Status == SuggestionStatus.Finished || s.Status == SuggestionStatus.Active)
            .OrderBy(s => s.ActiveAtUtc ?? s.FinishedAtUtc ?? s.CreatedAtUtc);

    private static DateTime StartOf(Suggestion game) =>
        game.ActiveAtUtc ?? game.FinishedAtUtc ?? game.CreatedAtUtc;
}
