namespace FunMasters.Shared.DTOs;

public class UpdateSuggestionRequest
{
    public string Title { get; set; } = null!;
    public int Order { get; set; }
    public bool IsHidden { get; set; }
    public string? CoverArtUrl { get; set; } // IGDB URL
    public string? SteamLink { get; set; }
}
