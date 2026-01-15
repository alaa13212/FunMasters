namespace FunMasters.Services;

using System.Net.Http;

public class GameCoverStorage
{
    private readonly IWebHostEnvironment _env;
    private readonly HttpClient _http;

    public GameCoverStorage(IWebHostEnvironment env, HttpClient http)
    {
        _env = env;
        _http = http;
    }

    private string GetFilePath(string fileName)
        => Path.Combine(_env.WebRootPath, "uploads", "gamecovers", fileName);

    public string GetPublicUrl(Guid gameId)
        => $"/uploads/gamecovers/{gameId:n}";

    public async Task<string> SaveCoverAsync(string sourceUrl, Guid gameId)
    {
        // ensure directory exists
        var dir = Path.Combine(_env.WebRootPath, "uploads", "gamecovers");
        Directory.CreateDirectory(dir);
        
        var fileName = $"{gameId:n}";
        var filePath = GetFilePath(fileName);

        if (!sourceUrl.Contains("https:"))
            sourceUrl = "https:" + sourceUrl;
        using var response = await _http.GetAsync(sourceUrl);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream);

        return GetPublicUrl(gameId);
    }
}
