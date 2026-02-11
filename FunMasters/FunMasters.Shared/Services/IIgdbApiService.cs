using FunMasters.Shared.DTOs;

namespace FunMasters.Shared.Services;

public interface IIgdbApiService
{
    Task<List<IgdbGameDto>> SearchGamesAsync(string query);
}
