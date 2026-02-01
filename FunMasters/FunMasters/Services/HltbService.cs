using System.Text.RegularExpressions;

namespace FunMasters.Services;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;



public class HltbService
{
    private readonly HttpClient _httpClient;
    private const string API_URL = "https://howlongtobeat.com/api/search";
    private string _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public HltbService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://howlongtobeat.com/");
    }

    public async Task<HltbGameResult> SearchGameAsync(string gameName, string? authToken = null)
    {
        if (string.IsNullOrWhiteSpace(gameName))
            throw new ArgumentException("Game name cannot be empty", nameof(gameName));

        // If no token provided, try to get one from the main page
        var searchPayload = new
        {
            searchType = "games",
            searchTerms = gameName.Split(' ').Where(IsWord).ToArray(),
            searchPage = 1,
            size = 20,
            searchOptions = new
            {
                games = new
                {
                    userId = 0,
                    platform = "",
                    sortCategory = "popular",
                    rangeCategory = "main",
                    rangeTime = new { min = (int?)null, max = (int?)null },
                    gameplay = new { perspective = "", flow = "", genre = "", difficulty = "" },
                    rangeYear = new { min = "", max = "" },
                    modifier = ""
                },
                users = new { sortCategory = "postcount" },
                lists = new { sortCategory = "follows" },
                filter = "",
                sort = 0,
                randomizer = 0
            },
            useCache = true
        };

        var jsonPayload = JsonSerializer.Serialize(searchPayload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            if (string.IsNullOrWhiteSpace(authToken))
            {
                authToken = await GetAuthTokenAsync();
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, API_URL);
            request.Content = content;
            request.Headers.Add("x-auth-token", authToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"HowLongToBeat API returned {response.StatusCode}: {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<SearchResponse>(responseJson);

            if (searchResponse?.Data == null || searchResponse.Data.Count == 0)
                return null;

            var firstGame = searchResponse.Data[0];

            return new HltbGameResult
            {
                GameId = firstGame.GameId,
                GameName = firstGame.GameName,
                MainStory = ConvertSecondsToHours(firstGame.CompMain),
                MainPlusExtras = ConvertSecondsToHours(firstGame.CompPlus),
                Completionist = ConvertSecondsToHours(firstGame.Comp100),
                GameUrl = $"https://howlongtobeat.com/game/{firstGame.GameId}"
            };
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("Failed to retrieve data from HowLongToBeat", ex);
        }
    }

    private async Task<string> GetAuthTokenAsync()
    {
        // Return cached token if still valid (valid for 1 hour)
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        try
        {
            var currentMS = (long)(DateTime.Now - DateTime.UnixEpoch).TotalMilliseconds;
            // Fetch the main page to extract the auth token from the JavaScript
            var response = await _httpClient.GetAsync($"https://howlongtobeat.com/api/search/init?t={currentMS}");
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            TokenClass token = JsonSerializer.Deserialize<TokenClass>(responseJson);
            

            if (token != null)
            {
                _cachedToken = token.Token;
                _tokenExpiry = DateTime.UtcNow.AddHours(1);
                return _cachedToken;
            }

            throw new Exception("Could not find auth token in HowLongToBeat page");
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to retrieve auth token from HowLongToBeat", ex);
        }
    }

    private double ConvertSecondsToHours(int seconds)
    {
        return seconds > 0 ? Math.Round(seconds / 3600.0, 1) : 0;
    }
    
    public static bool IsWord(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.Any(char.IsLetterOrDigit);
    }
}

public class HltbGameResult
{
    public int GameId { get; set; }
    public string GameName { get; set; }
    public double MainStory { get; set; }
    public double MainPlusExtras { get; set; }
    public double Completionist { get; set; }
    public string GameUrl { get; set; }

    public override string ToString()
    {
        return $"{GameName}\n" +
               $"Main Story: {(MainStory > 0 ? $"{MainStory} hours" : "N/A")}\n" +
               $"Main + Extras: {(MainPlusExtras > 0 ? $"{MainPlusExtras} hours" : "N/A")}\n" +
               $"Completionist: {(Completionist > 0 ? $"{Completionist} hours" : "N/A")}";
    }
}

// Internal classes for JSON deserialization
internal class SearchResponse
{
    [JsonPropertyName("data")] public List<GameData> Data { get; set; }
}

internal class GameData
{
    [JsonPropertyName("game_id")] public int GameId { get; set; }

    [JsonPropertyName("game_name")] public string GameName { get; set; }

    [JsonPropertyName("comp_main")] public int CompMain { get; set; }

    [JsonPropertyName("comp_plus")] public int CompPlus { get; set; }

    [JsonPropertyName("comp_all")] public int CompAll { get; set; }

    [JsonPropertyName("comp_100")] public int Comp100 { get; set; }
}

internal class TokenClass
{
    [JsonPropertyName("token")] public string Token { get; set; }
}