using System.Net.Http.Json;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;

namespace FunMasters.Client.Services;

public class HltbApiService(HttpClient http) : IHltbApiService
{
    public async Task<HltbGameResultDto?> SearchGameAsync(string name)
    {
        return await http.GetFromJsonAsync<HltbGameResultDto>($"/api/hltb/search?name={Uri.EscapeDataString(name)}");
    }
}
