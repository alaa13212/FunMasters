using FunMasters.Data;

namespace FunMasters.Services;

/// <summary>Who still holds a gem, by the same rule wherever Lucian asks.</summary>
public static class GemRules
{
    /// <summary>
    /// The reviews whose authors have not yet awarded their gem on this title. Only a member who
    /// delivered a verdict may award one, and never to their own review, so a lone reviewer has
    /// nobody to give it to and holds nothing.
    /// </summary>
    public static List<Rating> UnspentGemHolders(IEnumerable<Rating> ratings, IReadOnlySet<Guid> awardedByIds)
    {
        var all = ratings.ToList();
        return all.Count < 2
            ? []
            : all.Where(r => !awardedByIds.Contains(r.RaterId)).ToList();
    }
}
