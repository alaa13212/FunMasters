namespace FunMasters.Shared.DTOs;

/// <summary>
/// The caller's pending game that the queue will pick up next, and when.
/// </summary>
public class NextQueuedDto
{
    public Guid SuggestionId { get; set; }
    public DateTime QueuedAtUtc { get; set; }
}
