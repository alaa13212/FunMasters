namespace FunMasters.Shared.DTOs;

/// <summary>
/// The full layout of the caller's pending suggestions, each list top to bottom. Every pending
/// suggestion must appear exactly once across the two lists.
/// </summary>
public class ArrangeSuggestionsRequest
{
    public List<Guid> VisibleIds { get; set; } = [];
    public List<Guid> HiddenIds { get; set; } = [];
}
