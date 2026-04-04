using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;

namespace FunMasters.Services;

public class SteamApiService(SteamPlaytimeService steamPlaytimeService) : ISteamApiService
{
    public async Task<ApiResult<SteamResolveResult>> ResolveSteamIdAsync(string input)
    {
        var (steamId, displayName, error) = await steamPlaytimeService.ResolveSteamInputAsync(input);
        if (error != null)
            return ApiResult<SteamResolveResult>.Fail(error);

        return ApiResult<SteamResolveResult>.Ok(new SteamResolveResult
        {
            SteamId = steamId!,
            DisplayName = displayName!
        });
    }

    public async Task<ApiResult<List<SteamPlaytimeDto>>> RefreshPlaytimesAsync(Guid suggestionId)
    {
        var dtos = await steamPlaytimeService.RefreshAllPlaytimesAsync(suggestionId);
        return ApiResult<List<SteamPlaytimeDto>>.Ok(dtos);
    }
}
