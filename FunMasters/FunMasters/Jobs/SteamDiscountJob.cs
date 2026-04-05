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
        var telegram = scope.ServiceProvider.GetRequiredService<TelegramService>();

        var queuedWithSteam = await db.Suggestions
            .Where(s => s.Status == SuggestionStatus.Queued && s.SteamLink != null)
            .ToListAsync(stoppingToken);

        var activeMembers = await db.Users
            .Where(u => u.CycleOrder > 0 && u.SteamId != null)
            .ToListAsync(stoppingToken);

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

            if (price == null || price.DiscountPercent <= 0)
            {
                if (price != null && suggestion.LastKnownPriceSar != price.FinalPrice)
                {
                    suggestion.LastKnownPriceSar = price.FinalPrice;
                }
                continue;
            }

            // Only notify if price dropped since last check
            if (suggestion.LastKnownPriceSar.HasValue && price.FinalPrice >= suggestion.LastKnownPriceSar.Value)
            {
                _logger.LogDebug("Skipping notification for {Title} — price has not dropped further", suggestion.Title);
                continue;
            }

            // Skip if every active member already owns the game
            if (activeMembers.Count > 0 && await AllMembersOwnGameAsync(steamService, activeMembers, appId.Value))
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

        await telegram.SendMessageAsync(BuildMessage(toNotify));
        _logger.LogInformation("Sent discount notification for {Count} game(s): {Titles}",
            toNotify.Count, string.Join(", ", toNotify.Select(g => g.Title)));
    }

    private static string BuildMessage(List<DiscountedGame> games)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Esteemed members of <b>The Council of Fun Masters</b>,\n");

        if (games.Count == 1)
        {
            var g = games[0];
            sb.AppendLine($"It is my duty to inform you that <b>{EscapeHtml(g.Title)}</b>, " +
                          $"a title currently awaiting the Council's deliberation, has been marked down on Steam.\n");
            sb.AppendLine($"<s>{EscapeHtml(g.Price.InitialFormatted)}</s> → <b>{EscapeHtml(g.Price.FinalFormatted)}</b>  (-{g.Price.DiscountPercent}%)");
            sb.AppendLine($"https://store.steampowered.com/app/{g.AppId}");
        }
        else
        {
            sb.AppendLine($"It is my duty to inform you that {games.Count} titles currently awaiting " +
                          $"the Council's deliberation have been marked down on Steam.\n");
            foreach (var g in games)
            {
                sb.AppendLine($"▪ <b>{EscapeHtml(g.Title)}</b>");
                sb.AppendLine($"  <s>{EscapeHtml(g.Price.InitialFormatted)}</s> → <b>{EscapeHtml(g.Price.FinalFormatted)}</b>  (-{g.Price.DiscountPercent}%)");
                sb.AppendLine($"  https://store.steampowered.com/app/{g.AppId}");
            }
        }

        sb.AppendLine("\nThe Council would do well to act swiftly.");
        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");
        return sb.ToString();
    }

    private async Task<bool> AllMembersOwnGameAsync(SteamService steamService, List<ApplicationUser> members, int appId)
    {
        foreach (var member in members)
        {
            var playtime = await steamService.GetPlaytimeAsync(member.SteamId!, appId);
            if (playtime == null)
                return false;
        }
        return true;
    }

    private static string EscapeHtml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
