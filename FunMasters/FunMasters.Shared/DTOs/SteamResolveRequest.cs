namespace FunMasters.Shared.DTOs;

public class SteamResolveRequest
{
    public string Input { get; set; } = null!;
}

public class SteamResolveResult
{
    public string SteamId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
}
