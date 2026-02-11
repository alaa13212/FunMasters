namespace FunMasters.Shared.DTOs;

public class AdminUpdateSuggestionRequest
{
    public SuggestionStatus Status { get; set; }
    public DateTime? ActiveAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public int? CycleNumber { get; set; }
}
