using FunMasters.Extensions;

namespace FunMasters.Services;

public class BadgeStorage(IWebHostEnvironment env)
{
    private static readonly Dictionary<Guid, DateTime> Cache = new();

    private string GetFilePath(Guid badgeId)
        => Path.Combine(env.WebRootPath, "uploads", "badges", badgeId.ToString("n"));

    public string? GetPublicUrl(Guid badgeId)
    {
        var path = GetFilePath(badgeId);
        if (!File.Exists(path))
            return null;
        return $"/uploads/badges/{badgeId:n}?{GetFileTimestamp(badgeId)}";
    }

    private string GetFileTimestamp(Guid badgeId)
    {
        return Cache.GetOrSet(badgeId, key => File.GetLastWriteTimeUtc(GetFilePath(key))).Ticks.ToString();
    }

    public async Task<string> SaveBadgeImageAsync(Stream stream, Guid badgeId)
    {
        var dir = Path.Combine(env.WebRootPath, "uploads", "badges");
        Directory.CreateDirectory(dir);

        string filePath = GetFilePath(badgeId);
        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream);
        stream.Close();

        Cache[badgeId] = DateTime.UtcNow;
        return GetPublicUrl(badgeId)!;
    }
}
