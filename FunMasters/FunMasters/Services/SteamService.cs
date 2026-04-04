using System.Text.Json.Serialization;

namespace FunMasters.Services;

public class SteamService(HttpClient httpClient, IConfiguration configuration)
{
    private readonly string _apiKey = configuration["Steam:ApiKey"] ?? "";

    public async Task<(int playtimeForever, int? playtime2Weeks)?> GetPlaytimeAsync(string steamId, int appId)
    {
        var url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={_apiKey}&steamid={steamId}&format=json&include_played_free_games=1";

        var response = await httpClient.GetFromJsonAsync<SteamOwnedGamesResponse>(url);

        if (response?.Response?.Games == null)
            return null;

        var game = response.Response.Games.FirstOrDefault(g => g.AppId == appId);
        return game == null ? null : (game.PlaytimeForever, game.Playtime2Weeks);
    }

    public async Task<string?> ResolveVanityUrlAsync(string vanityName)
    {
        var url = $"https://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key={_apiKey}&vanityurl={Uri.EscapeDataString(vanityName)}&format=json";

        var response = await httpClient.GetFromJsonAsync<SteamVanityUrlResponse>(url);

        return response?.Response?.Success == 1 ? response.Response.SteamId : null;
    }

    public async Task<SteamPlayerSummary?> GetPlayerSummaryAsync(string steamId)
    {
        var url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={_apiKey}&steamids={steamId}&format=json";

        var response = await httpClient.GetFromJsonAsync<SteamPlayerSummariesResponse>(url);

        return response?.Response?.Players?.FirstOrDefault();
    }
}

public class SteamPlayerSummary
{
    [JsonPropertyName("steamid")]
    public string SteamId { get; set; } = null!;

    [JsonPropertyName("personaname")]
    public string PersonaName { get; set; } = null!;
}

internal class SteamOwnedGamesResponse
{
    [JsonPropertyName("response")]
    public SteamOwnedGamesData? Response { get; set; }
}

internal class SteamOwnedGamesData
{
    [JsonPropertyName("game_count")]
    public int GameCount { get; set; }

    [JsonPropertyName("games")]
    public List<SteamOwnedGame>? Games { get; set; }
}

internal class SteamOwnedGame
{
    [JsonPropertyName("appid")]
    public int AppId { get; set; }

    [JsonPropertyName("playtime_forever")]
    public int PlaytimeForever { get; set; }

    [JsonPropertyName("playtime_2weeks")]
    public int? Playtime2Weeks { get; set; }
}

internal class SteamVanityUrlResponse
{
    [JsonPropertyName("response")]
    public SteamVanityUrlData? Response { get; set; }
}

internal class SteamVanityUrlData
{
    [JsonPropertyName("steamid")]
    public string? SteamId { get; set; }

    [JsonPropertyName("success")]
    public int Success { get; set; }
}

internal class SteamPlayerSummariesResponse
{
    [JsonPropertyName("response")]
    public SteamPlayerSummariesData? Response { get; set; }
}

internal class SteamPlayerSummariesData
{
    [JsonPropertyName("players")]
    public List<SteamPlayerSummary>? Players { get; set; }
}
