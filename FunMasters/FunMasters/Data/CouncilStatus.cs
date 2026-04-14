namespace FunMasters.Data;

public enum CouncilStatus
{
    Active = 0,
    Candidate = 1,
    Excommunicated = 2,
    Executed = 3,
    Shadow = 4
}

public static class CouncilStatusRoles
{
    public static readonly List<CouncilStatus> CanSuggest = [CouncilStatus.Active];
    public static readonly List<CouncilStatus> ExtraFloatingSuggestions = [CouncilStatus.Candidate, CouncilStatus.Excommunicated];
    public static readonly List<CouncilStatus> InQueue = [CouncilStatus.Active];
    public static readonly List<CouncilStatus> MustReview = [CouncilStatus.Active, CouncilStatus.Excommunicated];
    public static readonly List<CouncilStatus> ReceiveNotifications = [CouncilStatus.Active, CouncilStatus.Candidate, CouncilStatus.Excommunicated];
    public static readonly List<CouncilStatus> ShamingNotifications = [CouncilStatus.Active, CouncilStatus.Excommunicated];
}
