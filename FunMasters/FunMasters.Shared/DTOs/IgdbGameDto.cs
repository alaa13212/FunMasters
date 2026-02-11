namespace FunMasters.Shared.DTOs;

public class IgdbGameDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string? CoverUrl { get; set; }
    public string? SteamUrl { get; set; }
}
