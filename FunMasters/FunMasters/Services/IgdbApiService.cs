using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;

namespace FunMasters.Services;

/// <summary>
/// Proxy service for IGDB API - keeps API secrets server-side
/// </summary>
public class IgdbApiService(IgdbService igdbService) : IIgdbApiService
{
    public async Task<List<IgdbGameDto>> SearchGamesAsync(string query)
    {
        var games = await igdbService.SearchGamesAsync(query, CancellationToken.None);

        return games.Select(g => new IgdbGameDto
        {
            Id = g.id,
            Name = g.name,
            CoverUrl = g.cover?.url,
            SteamUrl = g.websites?
                .FirstOrDefault(w => w.url?.Contains("store.steampowered.com") == true)
                ?.url
        }).ToList();
    }
}
