using System.Text.Json.Nodes;

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
    private const string ApiUrl = "https://howlongtobeat.com/api/find";
    private TokenClass? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public HltbService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://howlongtobeat.com/");
    }

    public async Task<HltbGameResult?> SearchGameAsync(string gameName)
    {
        if (string.IsNullOrWhiteSpace(gameName))
            throw new ArgumentException("Game name cannot be empty", nameof(gameName));

        var searchPayload = new JsonObject
        {
            ["searchType"] = "games",
            ["searchTerms"] = new JsonArray(
                gameName.Split(' ').Where(IsWord).Select(x => (JsonNode)x).ToArray()
            ),
            ["searchPage"] = 1,
            ["size"] = 20,
            ["searchOptions"] = new JsonObject
            {
                ["games"] = new JsonObject
                {
                    ["userId"] = 0,
                    ["platform"] = "",
                    ["sortCategory"] = "popular",
                    ["rangeCategory"] = "main",
                    ["rangeTime"] = new JsonObject
                    {
                        ["min"] = null,
                        ["max"] = null
                    },
                    ["gameplay"] = new JsonObject
                    {
                        ["perspective"] = "",
                        ["flow"] = "",
                        ["genre"] = "",
                        ["difficulty"] = ""
                    },
                    ["rangeYear"] = new JsonObject
                    {
                        ["min"] = "",
                        ["max"] = ""
                    },
                    ["modifier"] = ""
                },
                ["users"] = new JsonObject
                {
                    ["sortCategory"] = "postcount"
                },
                ["lists"] = new JsonObject
                {
                    ["sortCategory"] = "follows"
                },
                ["filter"] = "",
                ["sort"] = 0,
                ["randomizer"] = 0
            },
            ["useCache"] = true
        };

        try
        {
            TokenClass authToken = await GetAuthTokenAsync();

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Add("x-auth-token", authToken.Token);
            request.Headers.Add("x-hp-key", authToken.HpKey);
            request.Headers.Add("x-hp-val", authToken.HpValue);

            searchPayload[authToken.HpKey] = authToken.HpValue;
            
            string jsonPayload = searchPayload.ToJsonString();
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            
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
                CountCompleted = firstGame.CountCompleted,
                GameUrl = $"https://howlongtobeat.com/game/{firstGame.GameId}"
            };
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("Failed to retrieve data from HowLongToBeat", ex);
        }
    }

    private async Task<TokenClass> GetAuthTokenAsync()
    {
        // Return cached token if still valid (valid for 24 hour)
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        try
        {
            var currentMS = (long)(DateTime.Now - DateTime.UnixEpoch).TotalMilliseconds;
            // Fetch the main page to extract the auth token from the JavaScript
            var response = await _httpClient.GetAsync($"{ApiUrl}/init?t={currentMS}");
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            TokenClass? token = JsonSerializer.Deserialize<TokenClass>(responseJson);
            

            if (token != null)
            {
                _cachedToken = token;
                _tokenExpiry = DateTime.UtcNow.AddHours(24);
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
    public int CountCompleted { get; set; }

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
    [JsonPropertyName("count_comp")] public int CountCompleted { get; set; }
}

internal class TokenClass
{
    [JsonPropertyName("token")] public string Token { get; set; }
    [JsonPropertyName("hpKey")] public string HpKey { get; set; }
    [JsonPropertyName("hpVal")] public string HpValue { get; set; }
}