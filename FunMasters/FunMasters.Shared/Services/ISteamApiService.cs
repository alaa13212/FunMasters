using FunMasters.Shared.DTOs;

namespace FunMasters.Shared.Services;

public interface ISteamApiService
{
    Task<ApiResult<SteamResolveResult>> ResolveSteamIdAsync(string input);

    Task<ApiResult<List<SteamPlaytimeDto>>> RefreshPlaytimesAsync(Guid suggestionId);
}
