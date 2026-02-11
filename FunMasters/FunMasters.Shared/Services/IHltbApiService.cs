using FunMasters.Shared.DTOs;

namespace FunMasters.Shared.Services;

public interface IHltbApiService
{
    Task<HltbGameResultDto?> SearchGameAsync(string name);
}
