namespace FunMasters.Shared.DTOs;

public class CreateRatingRequest
{
    public Guid SuggestionId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public int? ManualPlaytimeMinutes { get; set; }
    public int? ManualPlaytime2WeeksMinutes { get; set; }
}
