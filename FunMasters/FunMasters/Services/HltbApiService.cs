using FunMasters.Data;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

/// <summary>
/// Proxy service for HowLongToBeat API with database-backed caching.
/// Stores HLTB times in the database with expiry:
/// - New games (Suggestion created less than 3 months ago): 1 day expiry
/// - Older games: 1 month expiry
/// Falls back to expired database values if the HLTB API fails.
/// </summary>
public class HltbApiService(
    HltbService hltbService,
    IDbContextFactory<ApplicationDbContext> dbFactory) : IHltbApiService
{
    public async Task<HltbGameResultDto?> SearchGameAsync(string name)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        var utcNow = DateTime.UtcNow;

        await using var db = await dbFactory.CreateDbContextAsync();

        // Try to find an existing non-expired cache entry
        var cached = await db.HltbCache
            .FirstOrDefaultAsync(h => h.Title == normalizedName);

        if (cached != null && cached.ExpiresAtUtc > utcNow)
        {
            return new HltbGameResultDto
            {
                MainStory = cached.MainStory,
                MainPlusExtras = cached.MainPlusExtras,
                Completionist = cached.Completionist,
                GameUrl = cached.GameUrl
            };
        }

        // Try the HLTB API
        try
        {
            var result = await hltbService.SearchGameAsync(name);
            if (result == null)
                return null;

            var resultDto = new HltbGameResultDto
            {
                GameUrl = result.GameUrl,
                MainStory = FormatHours(result.MainStory),
                MainPlusExtras = FormatHours(result.MainPlusExtras),
                Completionist = FormatHours(result.Completionist)
            };

            // Determine expiry based on whether the game suggestion is new (< 3 months)
            var suggestion = await db.Suggestions
                .Where(s => s.Title.ToLower() == normalizedName)
                .OrderBy(s => s.CreatedAtUtc)
                .FirstOrDefaultAsync();

            var isNewGame = suggestion != null && (utcNow - suggestion.CreatedAtUtc).TotalDays < 90;
            var expiry = isNewGame ? TimeSpan.FromDays(1) : TimeSpan.FromDays(180);

            if (cached != null)
            {
                cached.MainStory = resultDto.MainStory;
                cached.MainPlusExtras = resultDto.MainPlusExtras;
                cached.Completionist = resultDto.Completionist;
                cached.GameUrl = resultDto.GameUrl;
                cached.ExpiresAtUtc = utcNow.Add(expiry);
                cached.CreatedAtUtc = utcNow;
            }
            else
            {
                db.HltbCache.Add(new HltbCache
                {
                    Title = normalizedName,
                    MainStory = resultDto.MainStory,
                    MainPlusExtras = resultDto.MainPlusExtras,
                    Completionist = resultDto.Completionist,
                    GameUrl = resultDto.GameUrl,
                    ExpiresAtUtc = utcNow.Add(expiry),
                    CreatedAtUtc = utcNow
                });
            }

            await db.SaveChangesAsync();
            return resultDto;
        }
        catch (Exception)
        {
            // API failed — fall back to expired database values if available
            if (cached != null)
            {
                return new HltbGameResultDto
                {
                    MainStory = cached.MainStory,
                    MainPlusExtras = cached.MainPlusExtras,
                    Completionist = cached.Completionist,
                    GameUrl = cached.GameUrl
                };
            }

            return null;
        }
    }

    private static string FormatHours(double hours)
    {
        // Round to nearest 0.5
        double rounded = Math.Round(hours * 2, MidpointRounding.AwayFromZero) / 2;

        int whole = (int)Math.Floor(rounded);
        bool hasHalf = Math.Abs(rounded - whole - 0.5) < double.Epsilon;

        if (hasHalf)
            return whole > 0 ? $"{whole}\u00BD" : "\u00BD";

        return whole.ToString();
    }
}
