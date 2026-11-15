using FunMasters.Shared;

namespace FunMasters.Services;

/// <summary>
/// One example message per trigger for the Cabinet, built from fixed sample data by the same
/// Build* methods that compose the real messages.
/// </summary>
public static class LucianSamples
{
    private const string Title = "Hades";
    private const string NextTitle = "Outer Wilds";

    public static Dictionary<string, string> Build(string? titleLink)
    {
        var discounted = new List<(string Title, int AppId, SteamPriceInfo Price)>
        {
            (NextTitle, 753640, new SteamPriceInfo
            {
                DiscountPercent = 60,
                InitialFormatted = "SAR 89.00",
                FinalFormatted = "SAR 35.60"
            })
        };

        return new Dictionary<string, string>
        {
            [LucianTriggers.GameRotation] = LucianGalade.BuildGameRotationMessage(
                Title, [("Ali", 1260), ("Sara", 840), ("Omar", 95)], NextTitle, "Sara",
                "https://store.steampowered.com/app/753640"),
            [LucianTriggers.RatingReminder] = LucianGalade.BuildRatingReminderMessage(
                Title, 2, 3, ["Omar", "Khaled"], includeShortCommentMembers: false, isRepeat: false),
            [LucianTriggers.GemReminder] = LucianGalade.BuildGemReminderMessage(
                Title, LucianTriggers.GemReminderDays, LucianTriggers.TitleWriterDays - LucianTriggers.GemReminderDays,
                ["Omar"], ["Sara", "Khaled"], isRepeat: true),
            [LucianTriggers.ShortReviewNudge] = LucianGalade.BuildRatingReminderMessage(
                Title, 1, 10, ["Khaled"], includeShortCommentMembers: true, isRepeat: true),
            [LucianTriggers.AllVerdictsIn] = LucianGalade.BuildAllRatingsInMessage(
                Title, 8.4m, RatingUtils.GetRatingLabel(84), 5),
            [LucianTriggers.TitleWriter] = LucianGalade.BuildTitleWriterMessage(Title, "Ali", 3, titleLink),
            [LucianTriggers.CycleWriter] = LucianGalade.BuildCycleWriterMessage(4, "Sara", 7),
            [LucianTriggers.GemRoundup] = LucianGalade.BuildGemRoundupMessage(
                [("Ali", Title), ("Ali", Title), ("Sara", Title)]),
            [LucianTriggers.FinishEarly] = LucianGalade.BuildFinishEarlyMessage(
                Title, 3, new DateTime(2026, 11, 20)),
            [LucianTriggers.WeeklyBriefing] = LucianGalade.BuildWeeklyDigestMessage(
                Title, "8.4/10", "Ali", NextTitle, 4, 10, [("Sara", 300), ("Ali", 120)],
                "Omori", new DateTime(2026, 12, 4), 1, 2),
            [LucianTriggers.QuarterlyRecords] = LucianGalade.BuildQuarterlyDigestMessage(
                Title, 9.1m, "Omori", 4.2m, NextTitle, 3120, "Omori", null),
            [LucianTriggers.PlaytimeShaming] = LucianGalade.BuildPlaytimeShamingMessage(
                NextTitle, 2, ["Omar", "Khaled"], isRepeat: true),
            [LucianTriggers.NewMember] = LucianGalade.BuildNewMemberMessage("Khaled"),
            [LucianTriggers.NewSuggestion] = LucianGalade.BuildNewSuggestionMessage("Omar", "Omori"),
            [LucianTriggers.Discount] = LucianGalade.BuildDiscountMessage(discounted),
            [LucianTriggers.FreeTitle] = LucianGalade.BuildGameFreeMessage(
                NextTitle, "https://store.steampowered.com/app/753640"),
            [LucianTriggers.LongReview] = LucianGalade.BuildLongReviewMessage("Ali", Title, 412, titleLink),
            [LucianTriggers.VerdictRevised] = LucianGalade.BuildVerdictRevisedMessage("Ali", Title, 6.0m, 8.5m),
            [LucianTriggers.PlaytimeMilestone] = LucianGalade.BuildPlaytimeMilestoneMessage(
                NextTitle, "Sara", "10 Hours", 615),
            [LucianTriggers.SuggestionAmended] = LucianGalade.BuildSuggestionAmendedMessage("Omar", "Omori"),
            [LucianTriggers.SuggestionWithdrawn] = LucianGalade.BuildSuggestionWithdrawnMessage("Omar"),
            [LucianTriggers.DiscountEnded] = LucianGalade.BuildPriceBackUpMessage(NextTitle, "SAR 89.00"),
        };
    }
}
