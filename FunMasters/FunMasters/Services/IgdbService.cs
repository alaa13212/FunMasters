namespace FunMasters.Services;

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

    public async Task<string> GetAccessTokenAsync()
    {
        if (_token != null && DateTime.UtcNow < _tokenExpiry)
            return _token;

        var clientId = _config["IGDB:ClientId"];
        var clientSecret = _config["IGDB:ClientSecret"];
        var tokenResponse = await _http.PostAsync(
            $"https://id.twitch.tv/oauth2/token?client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials",
            null
        );

        var data = await tokenResponse.Content.ReadFromJsonAsync<IgdbAuthResponse>()
                   ?? throw new Exception("Failed to parse token response");

        _token = data.access_token;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(data.expires_in - 60); // refresh a bit early
        return _token!;
    }
    
    public async Task<List<IgdbGame>> SearchGamesAsync(string query)
    {
        var token = await GetAccessTokenAsync();
        var clientId = _config["IGDB:ClientId"];

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games");
        request.Headers.Add("Client-ID", clientId);
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = new StringContent(
            $"search \"{query}\"; fields id,name,cover; limit 8;",
            System.Text.Encoding.UTF8,
            "text/plain"
        );

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<List<IgdbGame>>(json) ?? [];
    }
    
    public async Task<List<string>> GetCoverUrlAsync(List<int> coverIds)
    {
        var token = await GetAccessTokenAsync();
        var clientId = _config["IGDB:ClientId"];

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/covers");
        request.Headers.Add("Client-ID", clientId);
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = new StringContent(
            $"fields url,image_id; where game = ({string.Join(',', coverIds)});",
            System.Text.Encoding.UTF8,
            "text/plain"
        );

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var covers = System.Text.Json.JsonSerializer.Deserialize<List<IgdbCover>>(json);
        
        return covers?
                   .Where(c => c.image_id != null)
                   .Select(c => $"https://images.igdb.com/igdb/image/upload/t_cover_big/{c.image_id}.jpg")
                   .ToList()
               ?? [];
    }

    public record IgdbCover(int id, string? image_id, string url);


}
