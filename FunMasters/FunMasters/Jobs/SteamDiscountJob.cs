using FunMasters.Data;
using FunMasters.Services;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Jobs;

public class SteamDiscountJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SteamDiscountJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(12);

    public SteamDiscountJob(IServiceProvider services, ILogger<SteamDiscountJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckDiscountsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Steam discounts");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private record DiscountedGame(string Title, int AppId, SteamPriceInfo Price);

    private async Task CheckDiscountsAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var steamStore = scope.ServiceProvider.GetRequiredService<SteamStoreService>();
        var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
        var lucian = scope.ServiceProvider.GetRequiredService<LucianGalade>();
        var telegram = scope.ServiceProvider.GetRequiredService<TelegramService>();

        var queuedWithSteam = await db.Suggestions
            .Where(s => s.Status == SuggestionStatus.Queued && s.SteamLink != null)
            .ToListAsync(stoppingToken);

        var activeMembers = await db.Users
            .Where(u => u.CycleOrder > 0 && CouncilStatusRoles.ReceiveNotifications.Contains(u.CouncilStatus) && u.SteamId != null)
            .ToListAsync(stoppingToken);

        var ownershipCache = new Dictionary<int, bool>();
        var toNotify = new List<DiscountedGame>();

        foreach (var suggestion in queuedWithSteam)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var appId = SteamStoreService.ExtractAppId(suggestion.SteamLink!);
            if (appId == null) continue;

            SteamPriceInfo? price;
            try
            {
                price = await steamStore.GetPriceAsync(appId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch Steam price for app {AppId} ({Title})", appId, suggestion.Title);
                continue;
            }

            // Check for price back up (previously discounted, now not)
            if (price == null || price.DiscountPercent <= 0)
            {
                if (suggestion.LastKnownPriceSar.HasValue)
                {
                    var previousPrice = suggestion.LastKnownPriceSar.Value;
                    if (price != null && price.FinalPrice > previousPrice)
                    {
                        if (!await CachedAllMembersOwnGameAsync(steamService, activeMembers, appId.Value, ownershipCache))
                        {
                            try { await lucian.SendPriceBackUpAsync(suggestion.Title, price.FinalFormatted); }
                            catch (Exception ex) { _logger.LogWarning(ex, "Failed to send price-back-up notification"); }
                        }
                    }
                }

                if (price != null && suggestion.LastKnownPriceSar != price.FinalPrice)
                {
                    suggestion.LastKnownPriceSar = price.FinalPrice;
                }
                continue;
            }

            // #8 Game Free notification (100% discount, price = 0)
            if (price is { DiscountPercent: 100, FinalPrice: 0 })
            {
                if (activeMembers.Count > 0 && !await CachedAllMembersOwnGameAsync(steamService, activeMembers, appId.Value, ownershipCache))
                {
                    suggestion.LastKnownPriceSar = 0;
                    try { await lucian.SendGameFreeAsync(suggestion.Title, $"https://store.steampowered.com/app/{appId.Value}"); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Failed to send game-free notification"); }
                    continue;
                }
            }

            // Only notify if price dropped since last check
            if (suggestion.LastKnownPriceSar.HasValue && price.FinalPrice >= suggestion.LastKnownPriceSar.Value)
            {
                _logger.LogDebug("Skipping notification for {Title} — price has not dropped further", suggestion.Title);
                continue;
            }

            // Skip if every active member already owns the game
            if (activeMembers.Count > 0 && await CachedAllMembersOwnGameAsync(steamService, activeMembers, appId.Value, ownershipCache))
            {
                _logger.LogInformation("Skipping notification for {Title} — all active members already own it", suggestion.Title);
                suggestion.LastKnownPriceSar = price.FinalPrice;
                continue;
            }

            suggestion.LastKnownPriceSar = price.FinalPrice;
            toNotify.Add(new DiscountedGame(suggestion.Title, appId.Value, price));
        }

        await db.SaveChangesAsync(stoppingToken);

        if (toNotify.Count == 0) return;

        var message = LucianGalade.BuildDiscountMessage(
            toNotify.Select(g => (g.Title, g.AppId, g.Price)).ToList());
        await telegram.SendMessageAsync(message);
        _logger.LogInformation("Sent discount notification for {Count} game(s): {Titles}",
            toNotify.Count, string.Join(", ", toNotify.Select(g => g.Title)));
    }

    private async Task<bool> CachedAllMembersOwnGameAsync(
        SteamService steamService, List<ApplicationUser> members, int appId, Dictionary<int, bool> cache)
    {
        if (cache.TryGetValue(appId, out var cached))
            return cached;

        var result = await AllMembersOwnGameAsync(steamService, members, appId);
        cache[appId] = result;
        return result;
    }

    private static async Task<bool> AllMembersOwnGameAsync(SteamService steamService, List<ApplicationUser> members, int appId)
    {
        foreach (var member in members)
        {
            var playtime = await steamService.GetPlaytimeAsync(member.SteamId!, appId);
            if (playtime == null)
                return false;
        }
        return true;
    }
}
