namespace FunMasters.Shared.DTOs;

public class HomePageDto
{
    public SuggestionDto? ActiveSuggestion { get; set; }
    public List<SuggestionDto> QueuedSuggestions { get; set; } = [];
    public List<SuggestionDto> FinishedSuggestions { get; set; } = [];
}
