using System.Net.Http.Json;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;

namespace FunMasters.Client.Services;

public class SteamApiService(HttpClient http) : ISteamApiService
{
    public async Task<ApiResult<SteamResolveResult>> ResolveSteamIdAsync(string input)
    {
        var response = await http.PostAsJsonAsync("/api/steam/resolve", new SteamResolveRequest { Input = input });
        return await response.Content.ReadFromJsonAsync<ApiResult<SteamResolveResult>>()
               ?? ApiResult<SteamResolveResult>.Fail("Failed to parse response");
    }

    public async Task<ApiResult<List<SteamPlaytimeDto>>> RefreshPlaytimesAsync(Guid suggestionId)
    {
        var response = await http.PostAsync($"/api/steam/refresh-playtimes/{suggestionId}", null);
        return await response.Content.ReadFromJsonAsync<ApiResult<List<SteamPlaytimeDto>>>()
               ?? ApiResult<List<SteamPlaytimeDto>>.Fail("Failed to parse response");
    }
}
