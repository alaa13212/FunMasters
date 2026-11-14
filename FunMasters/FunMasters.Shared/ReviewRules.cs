namespace FunMasters.Shared;

/// <summary>
/// How the Council measures a review, in one place: the server judges weak and long reviews by
/// these rules, and the review form counts words the same way, so the counter a member watches
/// while typing can never disagree with what Lucian later says about it.
/// </summary>
public static class ReviewRules
{
    /// <summary>A review with fewer words than this lacks substance.</summary>
    public const int MinReviewWords = 3;

    /// <summary>A review with at least this many words is announced by Lucian.</summary>
    public const int LongReviewWords = 150;

    /// <summary>Words separated by any whitespace, line breaks included.</summary>
    public static int CountWords(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    public static bool IsLong(string? text) => CountWords(text) >= LongReviewWords;
}
