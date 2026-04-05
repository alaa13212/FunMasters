using System.Text.Json;
using System.Text.RegularExpressions;

namespace FunMasters.Services;

public class SteamStoreService(HttpClient httpClient)
{
    public static int? ExtractAppId(string steamLink)
    {
        var match = Regex.Match(steamLink, @"/app/(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    public async Task<SteamPriceInfo?> GetPriceAsync(int appId)
    {
        var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=sa&filters=price_overview";

        var json = await httpClient.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty(appId.ToString(), out var appData))
            return null;

        if (!appData.TryGetProperty("success", out var success) || !success.GetBoolean())
            return null;

        if (!appData.TryGetProperty("data", out var data))
            return null;

        // Free games have no price_overview
        if (!data.TryGetProperty("price_overview", out var priceOverview))
            return null;

        return new SteamPriceInfo
        {
            DiscountPercent = priceOverview.GetProperty("discount_percent").GetInt32(),
            FinalPrice = priceOverview.GetProperty("final").GetInt32(),
            InitialPrice = priceOverview.GetProperty("initial").GetInt32(),
            FinalFormatted = priceOverview.GetProperty("final_formatted").GetString() ?? "",
            InitialFormatted = priceOverview.GetProperty("initial_formatted").GetString() ?? "",
        };
    }
}

public class SteamPriceInfo
{
    public int DiscountPercent { get; set; }
    public int FinalPrice { get; set; }
    public int InitialPrice { get; set; }
    public string FinalFormatted { get; set; } = "";
    public string InitialFormatted { get; set; } = "";
}
