namespace FunMasters.Services;

using System.Text.Json;
using System.Net.Http.Json;

public class IgdbService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private string? _token;
    private DateTime _tokenExpiry;

    public IgdbService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_token != null && DateTime.UtcNow < _tokenExpiry)
            return _token;

        var clientId = _config["IGDB:ClientId"];
        var clientSecret = _config["IGDB:ClientSecret"];
        var tokenResponse = await _http.PostAsync(
            $"https://id.twitch.tv/oauth2/token?client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials",
            null,
            cancellationToken
        );

        var data = await tokenResponse.Content.ReadFromJsonAsync<IgdbAuthResponse>()
                   ?? throw new Exception("Failed to parse token response");

        _token = data.access_token;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(data.expires_in - 60); // refresh a bit early
        return _token!;
    }
    
    public async Task<List<IgdbGame>> SearchGamesAsync(string query, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var clientId = _config["IGDB:ClientId"];

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games");
        request.Headers.Add("Client-ID", clientId);
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = new StringContent(
            $"search \"{query}\"; fields id,name,cover, cover.url,websites,websites.url; limit 8;",
            System.Text.Encoding.UTF8,
            "text/plain"
        );

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        List<IgdbGame> igdbGames = JsonSerializer.Deserialize<List<IgdbGame>>(json) ?? [];
        List<IgdbGame> igdbGamesWithCovers = igdbGames.Where(g => g.cover != null).ToList();
        foreach (IgdbGame game in igdbGamesWithCovers)
            game.cover!.url = game.cover.url?.Replace("t_thumb", "t_1080p");
        
        return igdbGamesWithCovers;
    }

}
