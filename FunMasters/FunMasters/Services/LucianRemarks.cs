namespace FunMasters.Services;

/// <summary>
/// What Lucian says on the website, as opposed to Telegram: plain sentences for the member and game
/// pages, pointing out problems. A page with nothing to point out gets no remarks at all.
/// </summary>
public static class LucianRemarks
{
    /// <summary>Lists longer than this end in "and N more" rather than filling the page.</summary>
    private const int MaxNamed = 5;

    public static List<string> ForMember(
        string userName,
        IReadOnlyList<string> missedVerdictTitles,
        int weakReviews,
        IReadOnlyList<string> unspentGemTitles)
    {
        var remarks = new List<string>();

        if (missedVerdictTitles.Count > 0)
            remarks.Add($"The Council notes with... interest... that {userName} has yet to render judgment on " +
                        $"{Join(missedVerdictTitles)}.");

        if (weakReviews == 1)
            remarks.Add($"One of {userName}'s verdicts lacks the substance befitting a Fun Master's review.");
        else if (weakReviews > 1)
            remarks.Add($"{weakReviews} of {userName}'s verdicts lack the substance befitting a Fun Master's review.");

        if (unspentGemTitles.Count > 0)
            remarks.Add($"{userName} still holds a gem for {Join(unspentGemTitles)}.");

        return remarks;
    }

    public static List<string> ForTitle(IReadOnlyList<string> unratedNames, IReadOnlyList<string> unspentGemNames)
    {
        var remarks = new List<string>();

        if (unratedNames.Count > 0)
            remarks.Add($"The Council notes with... interest... that {Join(unratedNames)} " +
                        $"{(unratedNames.Count == 1 ? "has" : "have")} yet to render judgment on this title.");

        if (unspentGemNames.Count > 0)
            remarks.Add($"{Join(unspentGemNames)} {(unspentGemNames.Count == 1 ? "has" : "have")} " +
                        "yet to award their gem on this title.");

        return remarks;
    }

    private static string Join(IReadOnlyList<string> items)
    {
        if (items.Count > MaxNamed)
            return string.Join(", ", items.Take(MaxNamed)) + $" and {items.Count - MaxNamed} more";

        return items.Count switch
        {
            1 => items[0],
            _ => string.Join(", ", items.Take(items.Count - 1)) + " and " + items[^1]
        };
    }
}
