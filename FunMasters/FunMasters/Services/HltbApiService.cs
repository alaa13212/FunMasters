using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;

namespace FunMasters.Services;

/// <summary>
/// Proxy service for HowLongToBeat API
/// </summary>
public class HltbApiService(HltbService hltbService) : IHltbApiService
{
    private static readonly Dictionary<string, CacheRecord> Cache = new();
    
    public async Task<HltbGameResultDto?> SearchGameAsync(string name)
    {
        if (Cache.TryGetValue(name, out CacheRecord? cacheRecord) && cacheRecord.ExpiresAt > DateTime.UtcNow)
            return cacheRecord.Result;
        
        var result = await hltbService.SearchGameAsync(name);
        if (result == null)
            return null;
        
        HltbGameResultDto resultDto = new HltbGameResultDto
        {
            GameUrl = result.GameUrl,
            MainStory = FormatHours(result.MainStory),
            MainPlusExtras = FormatHours(result.MainPlusExtras),
            Completionist = FormatHours(result.Completionist)
        };
        
        Cache[name] = new CacheRecord(DateTime.UtcNow.Add(ComputeCacheAge(result)), resultDto);
        return resultDto;
    }

    private TimeSpan ComputeCacheAge(HltbGameResult result)
    {
        return TimeSpan.FromDays(result.CountCompleted > 100 ? 14 : 1);
    }

    private static string FormatHours(double hours)
    {
        // Round to nearest 0.5
        double rounded = Math.Round(hours * 2, MidpointRounding.AwayFromZero) / 2;

        int whole = (int)Math.Floor(rounded);
        bool hasHalf = Math.Abs(rounded - whole - 0.5) < double.Epsilon;

        if (hasHalf)
            return whole > 0 ? $"{whole}½" : "1/2";

        return whole.ToString();
    }
    
    private record CacheRecord(DateTime ExpiresAt, HltbGameResultDto Result);
}
