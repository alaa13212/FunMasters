using FunMasters.Shared.DTOs;

namespace FunMasters.Shared.Services;

public interface IPublicDataApiService
{
    Task<PublicDataDto> GetPublicDataAsync();
}
