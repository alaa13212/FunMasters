using System.Net.Http.Json;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;

namespace FunMasters.Client.Services;

public class IgdbApiService(HttpClient http) : IIgdbApiService
{
    public async Task<List<IgdbGameDto>> SearchGamesAsync(string query)
    {
        return await http.GetFromJsonAsync<List<IgdbGameDto>>($"/api/igdb/search?q={Uri.EscapeDataString(query)}")
            ?? [];
    }
}
