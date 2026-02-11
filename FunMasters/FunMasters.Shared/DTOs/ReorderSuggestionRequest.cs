namespace FunMasters.Shared.DTOs;

public class ReorderSuggestionRequest
{
    public Guid SuggestionId { get; set; }
    public string Direction { get; set; } = null!; // "up" or "down"
}
