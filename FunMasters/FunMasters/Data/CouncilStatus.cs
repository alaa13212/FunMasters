namespace FunMasters.Data;

public static class CouncilStatusRoles
{
    public static readonly List<Shared.CouncilStatus> CanSuggest = [Shared.CouncilStatus.Active];
    public static readonly List<Shared.CouncilStatus> ExtraFloatingSuggestions = [Shared.CouncilStatus.Candidate, Shared.CouncilStatus.Excommunicated];
    public static readonly List<Shared.CouncilStatus> InQueue = [Shared.CouncilStatus.Active];
    public static readonly List<Shared.CouncilStatus> MustReview = [Shared.CouncilStatus.Active, Shared.CouncilStatus.Excommunicated];
    public static readonly List<Shared.CouncilStatus> ReceiveNotifications = [Shared.CouncilStatus.Active, Shared.CouncilStatus.Candidate, Shared.CouncilStatus.Excommunicated];
    public static readonly List<Shared.CouncilStatus> ShamingNotifications = [Shared.CouncilStatus.Active, Shared.CouncilStatus.Excommunicated];

    /// <summary>
    /// Members the Council has cast out. They stay on the roll as history, but behind everyone the
    /// Council is currently sitting - a struck-through name reads as an epitaph, not as a vacancy.
    /// </summary>
    public static readonly List<Shared.CouncilStatus> CastOut = [Shared.CouncilStatus.Excommunicated, Shared.CouncilStatus.Executed];

    /// <summary>
    /// Standings the roll leaves out entirely: an aspirant has not been admitted yet, and a Shadow
    /// is a spectator rather than a member, so neither belongs on a public list of members.
    /// </summary>
    public static readonly List<Shared.CouncilStatus> NotOnTheRoll = [Shared.CouncilStatus.Candidate, Shared.CouncilStatus.Shadow];
}