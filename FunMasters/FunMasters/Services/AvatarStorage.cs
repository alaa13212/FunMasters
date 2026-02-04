using FunMasters.Extensions;

namespace FunMasters.Services;

public class AvatarStorage(IWebHostEnvironment env, HttpClient http)
{
    private static readonly Dictionary<Guid, DateTime> Cache = new();

    private string GetFilePath(Guid gameId)
        => Path.Combine(env.WebRootPath, "uploads", "avatars", gameId.ToString("n"));

    public string GetPublicUrl(Guid userId)
        => $"/uploads/avatars/{userId:n}?{GetFileTimestamp(userId)}";

    private string GetFileTimestamp(Guid userId)
    {
        return Cache.GetOrSet(userId, key => File.GetLastWriteTimeUtc(GetFilePath(key))).Ticks.ToString();
    }

    public async Task<string> SaveAvatarsAsync(Stream stream, Guid userId)
    {
        // ensure directory exists
        var dir = Path.Combine(env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(dir);
        
        string filePath = GetFilePath(userId);
        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream);

        Cache[userId] = DateTime.UtcNow;

        return GetPublicUrl(userId);
    }
}