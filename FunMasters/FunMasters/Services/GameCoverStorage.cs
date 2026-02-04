using FunMasters.Extensions;

namespace FunMasters.Services;

using System.Net.Http;

public class GameCoverStorage(IWebHostEnvironment env, HttpClient http)
{
    private static readonly Dictionary<Guid, DateTime> Cache = new();

    private string GetFilePath(Guid gameId)
        => Path.Combine(env.WebRootPath, "uploads", "gamecovers", gameId.ToString("n"));

    public string GetPublicUrl(Guid gameId)
        => $"/uploads/gamecovers/{gameId:n}?{GetFileTimestamp(gameId)}";

    private string GetFileTimestamp(Guid gameId)
    {
        return Cache.GetOrSet(gameId, key => File.GetLastWriteTimeUtc(GetFilePath(key))).Ticks.ToString();
    }

    public async Task<string> SaveCoverAsync(string sourceUrl, Guid gameId)
    {
        // ensure directory exists
        var dir = Path.Combine(env.WebRootPath, "uploads", "gamecovers");
        Directory.CreateDirectory(dir);
        
        var fileName = $"{gameId:n}";
        var filePath = GetFilePath(gameId);

        if (!sourceUrl.Contains("https:"))
            sourceUrl = "https:" + sourceUrl;
        using var response = await http.GetAsync(sourceUrl);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream);

        Cache[gameId] = DateTime.UtcNow;

        return GetPublicUrl(gameId);
    }
}