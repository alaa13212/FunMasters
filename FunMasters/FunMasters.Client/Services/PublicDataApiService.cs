using System.Net.Http.Json;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;

namespace FunMasters.Client.Services;

public class PublicDataApiService(HttpClient http) : IPublicDataApiService
{
    public async Task<PublicDataDto> GetPublicDataAsync()
    {
        return await http.GetFromJsonAsync<PublicDataDto>("/api/public-data")
            ?? new PublicDataDto();
    }
}
