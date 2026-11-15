namespace FunMasters.Shared;

/// <summary>Whether a trigger speaks in Lucian's full frame or in a single signed line.</summary>
public enum LucianForm
{
    Full,
    OneLiner
}

public sealed record LucianTrigger(string Key, string Name, string WhenItFires, LucianForm Form);

/// <summary>
/// Everything that makes Lucian speak. The Cabinet page lists this catalogue, and every send in
/// LucianGalade is keyed by one of these entries, which is what lets an admin switch a trigger
/// off without a code change.
/// </summary>
public static class LucianTriggers
{
    public const string GameRotation = "game-rotation";
    public const string RatingReminder = "rating-reminder";
    public const string ShortReviewNudge = "short-review-nudge";
    public const string GemReminder = "gem-reminder";
    public const string AllVerdictsIn = "all-verdicts-in";
    public const string FinishEarly = "finish-early";
    public const string WeeklyBriefing = "weekly-briefing";
    public const string QuarterlyRecords = "quarterly-records";
    public const string PlaytimeShaming = "playtime-shaming";
    public const string NewMember = "new-member";
    public const string NewSuggestion = "new-suggestion";
    public const string Discount = "discount";
    public const string FreeTitle = "free-title";
    public const string GemRoundup = "gem-roundup";
    public const string TitleWriter = "title-writer";
    public const string CycleWriter = "cycle-writer";
    public const string PlaytimeMilestone = "playtime-milestone";
    public const string SuggestionAmended = "suggestion-amended";
    public const string SuggestionWithdrawn = "suggestion-withdrawn";
    public const string DiscountEnded = "discount-ended";
    public const string LongReview = "long-review";
    public const string VerdictRevised = "verdict-revised";

    /// <summary>Days after a title concludes before its Writer is named.</summary>
    public const int TitleWriterDays = 7;

    /// <summary>Days after a title concludes when Lucian chases verdicts and gems before the Writer.</summary>
    public const int GemReminderDays = 5;

    public static readonly IReadOnlyList<LucianTrigger> All =
    [
        new(GameRotation, "Game rotation",
            "When a deliberation period ends and the next title begins. Lists final playtimes and the incoming title, and waits for 9 AM.",
            LucianForm.Full),
        new(RatingReminder, "Rating reminder",
            $"Days 3 and 10 after a title concludes, at 9 AM, naming members yet to deliver a verdict. Day {GemReminderDays} belongs to the verdicts-and-gems reminder, and falls back to this one when that is switched off. From day {GemReminderDays} on, Anton Rayne is mentioned.",
            LucianForm.Full),
        new(GemReminder, "Verdicts and gems before the Writer",
            $"Day {GemReminderDays} after a title concludes, at 9 AM, in place of that day's rating reminder: who has not delivered a verdict, who has not awarded their gem, and that the Writer is named in {TitleWriterDays - GemReminderDays} days.",
            LucianForm.Full),
        new(ShortReviewNudge, "Short review nudge",
            $"Same schedule as the rating reminder, once every verdict is in, naming members whose review has fewer than {ReviewRules.MinReviewWords} words.",
            LucianForm.Full),
        new(AllVerdictsIn, "All verdicts in",
            "The moment the last expected verdict arrives: the average score and its label.",
            LucianForm.Full),
        new(TitleWriter, "Writer of the title",
            $"{TitleWriterDays} days after a title concludes, at 9 AM: the member whose review on it drew the most gems.",
            LucianForm.Full),
        new(CycleWriter, "Writer of the Cycle",
            "14 days after a cycle's last title concludes: the member whose reviews drew the most gems across the cycle.",
            LucianForm.Full),
        new(GemRoundup, "Gem roundup",
            "Every morning at 9 AM, the gems awarded the day before. Gems are anonymous, so only the receiver is named. A single gem goes out as a one-liner.",
            LucianForm.Full),
        new(FinishEarly, "Finish early",
            "When an admin ends a deliberation period early.",
            LucianForm.Full),
        new(WeeklyBriefing, "Weekly briefing",
            "Sundays at 8 PM: the last concluded title, the current one with playtime standings, the next one, and pending verdicts.",
            LucianForm.Full),
        new(QuarterlyRecords, "Quarterly records",
            "1 Jan, 1 Apr, 1 Jul and 1 Oct at 8 PM: highest and lowest rated, most played, most controversial.",
            LucianForm.Full),
        new(PlaytimeShaming, "Playtime shaming",
            "At the midpoint of a deliberation period and with 2 days left, naming members with no playtime on the current title.",
            LucianForm.Full),
        new(NewMember, "New member",
            "When an admin creates a new member.",
            LucianForm.Full),
        new(NewSuggestion, "New suggestion",
            "When a member submits a title.",
            LucianForm.Full),
        new(Discount, "Discount",
            "Checked every 12 hours: a title on the docket drops in price on Steam.",
            LucianForm.Full),
        new(FreeTitle, "Free title",
            "A title on the docket becomes free on Steam.",
            LucianForm.Full),
        new(LongReview, "Long review",
            $"When a review of {ReviewRules.LongReviewWords} words or more is delivered, or an edit makes one that long. Once per review. Reviews written while a title is still in deliberation are announced when it concludes.",
            LucianForm.OneLiner),
        new(VerdictRevised, "Verdict revised",
            "When a member changes their score on a concluded title by a full point or more.",
            LucianForm.OneLiner),
        new(PlaytimeMilestone, "Playtime milestone",
            "When a member passes 5, 10, 20 or 30 hours on the current title (checked every 6 hours).",
            LucianForm.OneLiner),
        new(SuggestionAmended, "Suggestion amended",
            "When a member edits their suggestion.",
            LucianForm.OneLiner),
        new(SuggestionWithdrawn, "Suggestion withdrawn",
            "When a member deletes a suggestion.",
            LucianForm.OneLiner),
        new(DiscountEnded, "Discount ended",
            "A discounted title on the docket returns to its full price.",
            LucianForm.OneLiner),
    ];

    public static bool Exists(string key) => All.Any(t => t.Key == key);
}

/// <summary>
/// How a custom alert is framed. Shared so the Cabinet can tell the sender which form their text
/// will take before they send it.
/// </summary>
public static class LucianAlertRules
{
    /// <summary>A single line up to this length goes out as a one-liner; anything else gets the full frame.</summary>
    public const int OneLinerMaxLength = 200;

    public static bool IsOneLiner(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length <= OneLinerMaxLength && !trimmed.Contains('\n');
    }
}
