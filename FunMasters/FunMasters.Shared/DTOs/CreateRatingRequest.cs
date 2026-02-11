namespace FunMasters.Shared.DTOs;

public class CreateRatingRequest
{
    public Guid SuggestionId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
}
