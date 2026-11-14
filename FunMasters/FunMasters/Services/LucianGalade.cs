using System.Text;
using FunMasters.Data;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

/// <summary>
/// Lucian Galade, Chief of Staff: every message the Council receives on Telegram is composed here.
/// Each message has a static Build* method that only formats, and the Send* methods add the trigger
/// switch and the transport. The Cabinet's sample messages call the same Build* methods, so what the
/// page shows is what Lucian actually says. Voice and formatting rules: docs/LUCIAN_GUIDE.md.
/// </summary>
public class LucianGalade(
    TelegramService telegram,
    ApplicationDbContext db,
    IConfiguration configuration,
    ILogger<LucianGalade> logger)
{
    private static readonly TimeSpan MorningHourUtc = TimeSpan.FromHours(6); // 6 AM UTC = 9 AM UTC+3

    private const string OpenerLine = "Esteemed members of the <b>Council of Fun Masters</b>,";
    public const string Signature = "<i>— Lucian Galade, Chief of Staff</i>";

    /// <summary>Ends every one-liner, in place of the opener and the full signature.</summary>
    public const string SignOff = "<i>L.G.</i>";

    private const string AntonRayneLine = "The Council would hate for Anton Rayne to take an interest in this matter.";
    private const string UrgentLine = "<b>[!] Urgent notice</b>";

    // ── Switches and transport ──────────────────────────────────────────────────────────────

    /// <summary>A trigger is on unless an admin switched it off in the Cabinet.</summary>
    public async Task<bool> IsEnabledAsync(string triggerKey)
    {
        var setting = await db.LucianTriggerSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == triggerKey);
        return setting?.Enabled ?? true;
    }

    private async Task SendAsync(string triggerKey, string message)
    {
        try
        {
            if (!await IsEnabledAsync(triggerKey))
            {
                logger.LogInformation("Lucian's {Trigger} trigger is switched off; message not sent", triggerKey);
                return;
            }

            await telegram.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Telegram notification ({Trigger})", triggerKey);
        }
    }

    public async Task QueueForMorningAsync(string triggerKey, string message)
    {
        if (!await IsEnabledAsync(triggerKey))
        {
            logger.LogInformation("Lucian's {Trigger} trigger is switched off; message not queued", triggerKey);
            return;
        }

        var now = FunMastersTime.UtcNow;
        var morningLocal = (now + FunMastersTime.UtcPlus3).Date;
        var sendAfter = morningLocal + MorningHourUtc - FunMastersTime.UtcPlus3;

        // If today's morning has passed, schedule for tomorrow
        if (sendAfter <= now)
            sendAfter = sendAfter.AddDays(1);

        db.PendingNotifications.Add(new PendingNotification
        {
            Message = message,
            SendAfterUtc = sendAfter
        });
        await db.SaveChangesAsync();
    }

    /// <summary>The title's page on the site, or null when the site's address is not configured.</summary>
    public string? TitleLink(Guid suggestionId)
    {
        var baseUrl = configuration["Site:BaseUrl"];
        return string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl.TrimEnd('/')}/game/{suggestionId}";
    }

    // ── Sends ───────────────────────────────────────────────────────────────────────────────

    public Task SendRatingReminderAsync(
        string gameTitle,
        int unratedCount,
        int daysSinceFinish,
        IEnumerable<string> unratedMemberNames,
        bool includeShortCommentMembers,
        bool isRepeat = false) =>
        SendAsync(
            includeShortCommentMembers ? LucianTriggers.ShortReviewNudge : LucianTriggers.RatingReminder,
            BuildRatingReminderMessage(gameTitle, unratedCount, daysSinceFinish, unratedMemberNames,
                includeShortCommentMembers, isRepeat));

    public Task SendGemReminderAsync(
        string gameTitle,
        int daysSinceFinish,
        int daysUntilWriter,
        IEnumerable<string> unratedMemberNames,
        IEnumerable<string> unspentGemMemberNames,
        bool isRepeat) =>
        SendAsync(LucianTriggers.GemReminder,
            BuildGemReminderMessage(gameTitle, daysSinceFinish, daysUntilWriter, unratedMemberNames,
                unspentGemMemberNames, isRepeat));

    public Task SendAllRatingsInAsync(string gameTitle, decimal averageScore, string ratingLabel, int ratingsCount) =>
        SendAsync(LucianTriggers.AllVerdictsIn,
            BuildAllRatingsInMessage(gameTitle, averageScore, ratingLabel, ratingsCount));

    public Task SendFinishEarlyAsync(string gameTitle, int daysCut, DateTime newEndDate) =>
        SendAsync(LucianTriggers.FinishEarly, BuildFinishEarlyMessage(gameTitle, daysCut, newEndDate));

    public Task SendWeeklyDigestAsync(
        string? finishedGameTitle,
        string? finishedGameAverageRating,
        string? finishedGameMostPlayed,
        string? activeGameTitle,
        int? activeDaysElapsed,
        int? activeDaysRemaining,
        IEnumerable<(string UserName, int PlaytimeMinutes)>? activePlaytimes,
        string? nextGameTitle,
        DateTime? nextGameStart,
        int finishedPendingRatingsCount,
        int overallPendingRatingsCount) =>
        SendAsync(LucianTriggers.WeeklyBriefing, BuildWeeklyDigestMessage(
            finishedGameTitle, finishedGameAverageRating, finishedGameMostPlayed, activeGameTitle,
            activeDaysElapsed, activeDaysRemaining, activePlaytimes, nextGameTitle, nextGameStart,
            finishedPendingRatingsCount, overallPendingRatingsCount));

    public async Task SendPlaytimeShamingAsync(
        string gameTitle,
        int daysRemaining,
        IEnumerable<string> absentMembers,
        bool isRepeat = false)
    {
        var members = absentMembers.ToList();
        if (members.Count == 0) return;

        await SendAsync(LucianTriggers.PlaytimeShaming,
            BuildPlaytimeShamingMessage(gameTitle, daysRemaining, members, isRepeat));
    }

    public Task SendPlaytimeMilestoneAsync(string gameTitle, string userName, string milestone, int totalMinutes) =>
        SendAsync(LucianTriggers.PlaytimeMilestone,
            BuildPlaytimeMilestoneMessage(gameTitle, userName, milestone, totalMinutes));

    public Task SendNewMemberAsync(string userName) =>
        SendAsync(LucianTriggers.NewMember, BuildNewMemberMessage(userName));

    public Task SendNewSuggestionAsync(string userName, string gameTitle) =>
        SendAsync(LucianTriggers.NewSuggestion, BuildNewSuggestionMessage(userName, gameTitle));

    public Task SendSuggestionEditedAsync(string userName, string newTitle) =>
        SendAsync(LucianTriggers.SuggestionAmended, BuildSuggestionAmendedMessage(userName, newTitle));

    public Task SendSuggestionDeletedAsync(string userName) =>
        SendAsync(LucianTriggers.SuggestionWithdrawn, BuildSuggestionWithdrawnMessage(userName));

    public Task SendPriceBackUpAsync(string gameTitle, string originalPrice) =>
        SendAsync(LucianTriggers.DiscountEnded, BuildPriceBackUpMessage(gameTitle, originalPrice));

    public Task SendGameFreeAsync(string gameTitle, string steamLink) =>
        SendAsync(LucianTriggers.FreeTitle, BuildGameFreeMessage(gameTitle, steamLink));

    public Task SendDiscountAsync(List<(string Title, int AppId, SteamPriceInfo Price)> games) =>
        SendAsync(LucianTriggers.Discount, BuildDiscountMessage(games));

    public Task SendQuarterlyDigestAsync(
        string topRatedTitle,
        decimal topRatedScore,
        string lowestRatedTitle,
        decimal lowestRatedScore,
        string mostPlayedTitle,
        int mostPlayedMinutes,
        string mostControversialTitle,
        string? mostDivisiveTitle) =>
        SendAsync(LucianTriggers.QuarterlyRecords, BuildQuarterlyDigestMessage(
            topRatedTitle, topRatedScore, lowestRatedTitle, lowestRatedScore, mostPlayedTitle,
            mostPlayedMinutes, mostControversialTitle, mostDivisiveTitle));

    public Task SendTitleWriterAsync(Guid suggestionId, string gameTitle, string writerName, int gemCount) =>
        SendAsync(LucianTriggers.TitleWriter,
            BuildTitleWriterMessage(gameTitle, writerName, gemCount, TitleLink(suggestionId)));

    public Task SendCycleWriterAsync(int cycleNumber, string writerName, int gemCount) =>
        SendAsync(LucianTriggers.CycleWriter, BuildCycleWriterMessage(cycleNumber, writerName, gemCount));

    public async Task SendGemRoundupAsync(IReadOnlyList<(string ReceiverName, string GameTitle)> gems)
    {
        if (gems.Count == 0) return;
        await SendAsync(LucianTriggers.GemRoundup, BuildGemRoundupMessage(gems));
    }

    public Task SendVerdictRevisedAsync(string raterName, string gameTitle, decimal oldScore, decimal newScore) =>
        SendAsync(LucianTriggers.VerdictRevised,
            BuildVerdictRevisedMessage(raterName, gameTitle, oldScore, newScore));

    /// <summary>
    /// Announces a long review once. The rating must be tracked by the request's context: it is
    /// stamped before the message goes out, so a retry or a second edit can never announce it twice.
    /// Nothing is stamped while the trigger is off, and the caller decides whether the review is long.
    /// With <paramref name="holdForMorning"/>, the message joins the morning queue instead, behind
    /// anything already waiting there.
    /// </summary>
    public async Task AnnounceLongReviewAsync(Rating rating, string raterName, string gameTitle, bool holdForMorning = false)
    {
        if (rating.LongReviewAnnouncedAtUtc != null) return;
        if (!await IsEnabledAsync(LucianTriggers.LongReview)) return;

        rating.LongReviewAnnouncedAtUtc = FunMastersTime.UtcNow;
        await db.SaveChangesAsync();

        var message = BuildLongReviewMessage(
            raterName, gameTitle, ReviewRules.CountWords(rating.Comment), TitleLink(rating.SuggestionId));

        if (holdForMorning)
            await QueueForMorningAsync(LucianTriggers.LongReview, message);
        else
            await SendAsync(LucianTriggers.LongReview, message);
    }

    /// <summary>
    /// A member's custom alert. Not a trigger, so no switch applies. Failures propagate: the sender
    /// is waiting on the result.
    /// </summary>
    public async Task SendAlertAsync(string message, Stream? imageStream, string? imageFileName, string? imageContentType)
    {
        if (imageStream != null)
            await telegram.SendPhotoAsync(message, imageStream, imageFileName ?? "image", imageContentType!);
        else
            await telegram.SendMessageAsync(message);
    }

    // ── Builders: existing triggers ─────────────────────────────────────────────────────────

    public static string BuildRatingReminderMessage(
        string gameTitle,
        int unratedCount,
        int daysSinceFinish,
        IEnumerable<string> unratedMemberNames,
        bool includeShortCommentMembers,
        bool isRepeat)
    {
        var sb = Open();

        if (!includeShortCommentMembers)
        {
            sb.AppendLine($"It has been <b>{daysSinceFinish} days</b> since the Council concluded its deliberation " +
                          $"of <b>{Escape(gameTitle)}</b>.");
            sb.AppendLine($"<b>{unratedCount}</b> member(s) have yet to deliver their verdict. " +
                          "The Council grows impatient.\n");

            sb.AppendLine("Those yet to render judgment:");
            AppendBullets(sb, unratedMemberNames);
        }
        else
        {
            sb.AppendLine($"A gentle reminder regarding <b>{Escape(gameTitle)}</b>: " +
                          "the Council notes that some verdicts, while delivered, lack the substance befitting a Fun Master's review. " +
                          $"A verdict with fewer than {NumberWord(ReviewRules.MinReviewWords)} words hardly does justice to the deliberation.\n");
            sb.AppendLine("The Council encourages these members to elaborate:");
            AppendBullets(sb, unratedMemberNames);
        }

        if (isRepeat)
        {
            sb.AppendLine();
            sb.AppendLine(AntonRayneLine);
        }

        sb.AppendLine("\nThe Council awaits.");
        return Close(sb);
    }

    public static string BuildAllRatingsInMessage(string gameTitle, decimal averageScore, string ratingLabel, int ratingsCount)
    {
        var sb = Open();
        sb.AppendLine($"The Council has reached consensus on <b>{Escape(gameTitle)}</b>.\n");
        sb.AppendLine($"<b>{averageScore:F1}/10</b> — <i>{Escape(ratingLabel)}</i>");
        sb.AppendLine($"{ratingsCount} verdict(s) delivered.");
        sb.AppendLine("\nThe record stands.");
        return Close(sb);
    }

    public static string BuildFinishEarlyMessage(string gameTitle, int daysCut, DateTime newEndDate)
    {
        var sb = Open();
        sb.AppendLine($"By order of the Chair, the deliberation period for <b>{Escape(gameTitle)}</b> " +
                      $"has been shortened by <b>{daysCut} day(s)</b>.\n");
        sb.AppendLine($"The new conclusion date is <b>{newEndDate:dd MMM yyyy}</b>. " +
                      "Final playtimes will be captured, and the next title shall commence shortly.");
        sb.AppendLine("\nThe Council adjusts its schedule accordingly.");
        return Close(sb);
    }

    public static string BuildWeeklyDigestMessage(
        string? finishedGameTitle,
        string? finishedGameAverageRating,
        string? finishedGameMostPlayed,
        string? activeGameTitle,
        int? activeDaysElapsed,
        int? activeDaysRemaining,
        IEnumerable<(string UserName, int PlaytimeMinutes)>? activePlaytimes,
        string? nextGameTitle,
        DateTime? nextGameStart,
        int finishedPendingRatingsCount,
        int overallPendingRatingsCount)
    {
        var sb = Open();
        sb.AppendLine("The Council's weekly briefing follows.\n");

        if (finishedGameTitle != null)
        {
            sb.AppendLine($"<b>Concluded:</b> {Escape(finishedGameTitle)}");
            if (finishedGameAverageRating != null)
            {
                string pendingReview = finishedPendingRatingsCount > 0 ? $" ({finishedPendingRatingsCount} rating(s) missing)" : "";
                sb.AppendLine($"  Average Rating: {finishedGameAverageRating}{pendingReview}");
            }

            if (finishedGameMostPlayed != null)
                sb.AppendLine($"  Most Dedicated: {Escape(finishedGameMostPlayed)}");
            sb.AppendLine();
        }

        if (activeGameTitle != null)
        {
            sb.AppendLine($"<b>Currently in Deliberation:</b> {Escape(activeGameTitle)}");
            if (activeDaysElapsed.HasValue && activeDaysRemaining.HasValue)
                sb.AppendLine($"  Day {activeDaysElapsed.Value} of {activeDaysElapsed.Value + activeDaysRemaining.Value}");
            if (activePlaytimes != null)
            {
                var pts = activePlaytimes.OrderByDescending(p => p.PlaytimeMinutes).ToList();
                if (pts.Count > 0)
                {
                    sb.AppendLine("  Playtime standings:");
                    foreach (var (userName, minutes) in pts)
                        sb.AppendLine($"    ▪ {Escape(userName)}: {FormatPlaytime(minutes)}");
                }
            }
            sb.AppendLine();
        }

        if (nextGameTitle != null && nextGameStart.HasValue)
        {
            sb.AppendLine($"<b>Up Next:</b> {Escape(nextGameTitle)} — commencing {nextGameStart.Value:dd MMM yyyy}");
            sb.AppendLine();
        }

        if (overallPendingRatingsCount > 0)
            sb.AppendLine($"<b>Pending Verdicts:</b> {overallPendingRatingsCount} unfinished rating(s) await the Council's attention.");

        sb.AppendLine("\nThe Council is advised to stay current.");
        return Close(sb);
    }

    public static string BuildPlaytimeShamingMessage(
        string gameTitle,
        int daysRemaining,
        IReadOnlyList<string> absentMembers,
        bool isRepeat)
    {
        var sb = Open();

        if (absentMembers.Count == 1)
        {
            sb.AppendLine($"{Escape(absentMembers[0])}, the Council notes with... interest... " +
                          $"that you have yet to begin <b>{Escape(gameTitle)}</b>.");
        }
        else
        {
            sb.AppendLine("The Council notes with... interest... that the following members have " +
                          $"yet to begin <b>{Escape(gameTitle)}</b>:");
            AppendBullets(sb, absentMembers);
        }

        sb.AppendLine($"\nOnly <b>{daysRemaining} day(s)</b> remain in the deliberation period.");

        if (isRepeat)
            sb.AppendLine("\n" + AntonRayneLine);

        return Close(sb);
    }

    public static string BuildPlaytimeMilestoneMessage(string gameTitle, string userName, string milestone, int totalMinutes) =>
        OneLine($"The Council observes that <b>{Escape(userName)}</b> has reached the <b>{Escape(milestone)}</b> milestone " +
                $"in <b>{Escape(gameTitle)}</b>, with {FormatPlaytime(totalMinutes)} dedicated. Duly noted.");

    public static string BuildNewMemberMessage(string userName)
    {
        var sb = Open();
        sb.AppendLine($"The Council welcomes its newest member, <b>{Escape(userName)}</b>. " +
                      "May their suggestions be ever inspired and their playtime plentiful.");
        sb.AppendLine("\nA seat has been prepared.");
        return Close(sb);
    }

    public static string BuildNewSuggestionMessage(string userName, string gameTitle)
    {
        var sb = Open();
        sb.AppendLine($"{Escape(userName)} has submitted <b>{Escape(gameTitle)}</b> for the Council's consideration.");
        sb.AppendLine("\nThe docket has been updated.");
        return Close(sb);
    }

    public static string BuildSuggestionAmendedMessage(string userName, string newTitle) =>
        OneLine($"<b>{Escape(userName)}</b> has amended their proposal: <b>{Escape(newTitle)}</b>. The docket has been updated.");

    public static string BuildSuggestionWithdrawnMessage(string userName) =>
        OneLine($"<b>{Escape(userName)}</b> has withdrawn a proposal from the Council's consideration. The docket has been updated.");

    public static string BuildPriceBackUpMessage(string gameTitle, string originalPrice) =>
        OneLine($"The discount on <b>{Escape(gameTitle)}</b> has ended; the title returns to its standard price of " +
                $"{Escape(originalPrice)}. The Council may wish they had acted sooner.");

    public static string BuildGameFreeMessage(string gameTitle, string steamLink)
    {
        var sb = Open();
        sb.AppendLine($"A rare occurrence: <b>{Escape(gameTitle)}</b> — a title currently awaiting " +
                      "the Council's deliberation — is now <b>FREE</b> on Steam.\n");

        if (!string.IsNullOrWhiteSpace(steamLink))
            sb.AppendLine(steamLink);

        sb.AppendLine("\nThe Council would do well to act swiftly.");
        return Close(sb);
    }

    public static string BuildQuarterlyDigestMessage(
        string topRatedTitle,
        decimal topRatedScore,
        string lowestRatedTitle,
        decimal lowestRatedScore,
        string mostPlayedTitle,
        int mostPlayedMinutes,
        string mostControversialTitle,
        string? mostDivisiveTitle)
    {
        var sb = Open();
        sb.AppendLine("As the season turns, the Council's records have been reviewed. A summary of distinction follows.\n");

        sb.AppendLine($"<b>Highest Acclaim:</b> {Escape(topRatedTitle)} — {topRatedScore:F1}/10");
        sb.AppendLine($"<b>Lowest Regard:</b> {Escape(lowestRatedTitle)} — {lowestRatedScore:F1}/10");
        sb.AppendLine($"<b>Most Deliberated:</b> {Escape(mostPlayedTitle)} — {FormatPlaytime(mostPlayedMinutes)} collectively invested");
        sb.AppendLine($"<b>Most Controversial:</b> {Escape(mostControversialTitle)} — the widest divergence of opinion");

        if (mostDivisiveTitle != null)
            sb.AppendLine($"<b>Most Divisive:</b> {Escape(mostDivisiveTitle)} — love or hate, no middle ground");

        sb.AppendLine("\nThe Council's legacy endures.");
        return Close(sb);
    }

    public static string BuildGameRotationMessage(
        string outgoingTitle,
        IEnumerable<(string UserName, int PlaytimeMinutes)> outgoingPlaytimes,
        string incomingTitle,
        string incomingSuggester,
        string? incomingSteamLink)
    {
        var sb = Open();
        sb.AppendLine($"The Council's deliberation of <b>{Escape(outgoingTitle)}</b> has concluded.\n");

        var playtimes = outgoingPlaytimes.ToList();
        if (playtimes.Count > 0)
        {
            sb.AppendLine("Final playtime standings:");
            foreach (var (userName, minutes) in playtimes.OrderByDescending(p => p.PlaytimeMinutes))
                sb.AppendLine($"  ▪ {Escape(userName)}: {FormatPlaytime(minutes)}");
            sb.AppendLine();
        }

        sb.AppendLine($"The gavel falls. The next title before the Council is <b>{Escape(incomingTitle)}</b>, " +
                      $"nominated by {Escape(incomingSuggester)}.");

        if (!string.IsNullOrWhiteSpace(incomingSteamLink))
            sb.AppendLine(incomingSteamLink);

        sb.AppendLine("\nMay your deliberations be thorough.");
        return Close(sb);
    }

    public static string BuildDiscountMessage(List<(string Title, int AppId, SteamPriceInfo Price)> games)
    {
        var sb = Open();

        if (games.Count == 1)
        {
            var g = games[0];
            sb.AppendLine($"It is my duty to inform you that <b>{Escape(g.Title)}</b>, " +
                          $"a title currently awaiting the Council's deliberation, has been marked down on Steam.\n");
            sb.AppendLine($"<s>{Escape(g.Price.InitialFormatted)}</s> → <b>{Escape(g.Price.FinalFormatted)}</b>  (-{g.Price.DiscountPercent}%)");
            sb.AppendLine($"https://store.steampowered.com/app/{g.AppId}");
        }
        else
        {
            sb.AppendLine($"It is my duty to inform you that {games.Count} titles currently awaiting " +
                          $"the Council's deliberation have been marked down on Steam.\n");
            foreach (var g in games)
            {
                sb.AppendLine($"▪ <b>{Escape(g.Title)}</b>");
                sb.AppendLine($"  <s>{Escape(g.Price.InitialFormatted)}</s> → <b>{Escape(g.Price.FinalFormatted)}</b>  (-{g.Price.DiscountPercent}%)");
                sb.AppendLine($"  https://store.steampowered.com/app/{g.AppId}");
            }
        }

        sb.AppendLine("\nThe Council would do well to act swiftly.");
        return Close(sb);
    }

    // ── Builders: gems, Writers and reviews ─────────────────────────────────────────────────

    public static string BuildGemReminderMessage(
        string gameTitle,
        int daysSinceFinish,
        int daysUntilWriter,
        IEnumerable<string> unratedMemberNames,
        IEnumerable<string> unspentGemMemberNames,
        bool isRepeat)
    {
        var unrated = unratedMemberNames.ToList();
        var unspent = unspentGemMemberNames.ToList();
        var sb = Open();

        sb.AppendLine($"The Council concluded its deliberation of <b>{Escape(gameTitle)}</b> {daysSinceFinish} days ago. " +
                      $"In <b>{daysUntilWriter} day(s)</b>, it names the Writer of this title.");

        if (unrated.Count > 0)
        {
            sb.AppendLine("\nThose yet to deliver their verdict:");
            AppendBullets(sb, unrated);
        }

        if (unspent.Count > 0)
        {
            sb.AppendLine("\nThose yet to award their gem:");
            AppendBullets(sb, unspent);
        }

        if (isRepeat)
            sb.AppendLine("\n" + AntonRayneLine);

        sb.AppendLine("\nThe Council awaits.");
        return Close(sb);
    }

    public static string BuildTitleWriterMessage(string gameTitle, string writerName, int gemCount, string? link)
    {
        var sb = Open();
        sb.AppendLine($"The gems for <b>{Escape(gameTitle)}</b> have been counted.");
        sb.AppendLine($"<b>{Escape(writerName)}</b> is named Writer of <b>{Escape(gameTitle)}</b>, " +
                      $"their review having drawn <b>{gemCount}</b> gem(s).");
        if (link != null)
            sb.AppendLine(link);
        sb.AppendLine("\nThe record stands.");
        return Close(sb);
    }

    public static string BuildCycleWriterMessage(int cycleNumber, string writerName, int gemCount)
    {
        var sb = Open();
        sb.AppendLine($"The gems of cycle <b>{cycleNumber}</b> have been counted.");
        sb.AppendLine($"<b>{Escape(writerName)}</b> is named Writer of the Cycle, their reviews having drawn " +
                      $"<b>{gemCount}</b> gem(s) across the cycle's titles.");
        sb.AppendLine("\nThe Council's legacy endures.");
        return Close(sb);
    }

    /// <summary>
    /// Gems are anonymous, so only the receiver is named. One gem is a one-liner; several are a
    /// list, with a review that drew more than one counted once.
    /// </summary>
    public static string BuildGemRoundupMessage(IReadOnlyList<(string ReceiverName, string GameTitle)> gems)
    {
        if (gems.Count == 1)
        {
            var (receiver, title) = gems[0];
            return OneLine($"<b>{Escape(receiver)}</b> received a gem yesterday for their review of <b>{Escape(title)}</b>. Duly noted.");
        }

        var sb = Open();
        sb.AppendLine("Yesterday, the following received a gem:");
        foreach (var group in gems.GroupBy(g => g).OrderBy(g => g.Key.GameTitle).ThenBy(g => g.Key.ReceiverName))
        {
            var count = group.Count() > 1 ? $" (<b>{group.Count()}</b> gems)" : "";
            sb.AppendLine($"  ▪ <b>{Escape(group.Key.ReceiverName)}</b>, for their review of <b>{Escape(group.Key.GameTitle)}</b>{count}");
        }
        sb.AppendLine("\nDuly noted.");
        return Close(sb);
    }

    public static string BuildLongReviewMessage(string raterName, string gameTitle, int wordCount, string? link)
    {
        var line = OneLine($"<b>{Escape(raterName)}</b> has delivered a review of <b>{wordCount} words</b> on " +
                           $"<b>{Escape(gameTitle)}</b>. Those who find it worthy may award a gem 💎.");
        return link == null ? line : line + "\n" + link;
    }

    public static string BuildVerdictRevisedMessage(string raterName, string gameTitle, decimal oldScore, decimal newScore) =>
        OneLine($"<b>{Escape(raterName)}</b> has revised their verdict on <b>{Escape(gameTitle)}</b> " +
                $"from <b>{oldScore:0.0}</b> to <b>{newScore:0.0}</b>. Duly noted.");

    /// <summary>
    /// A member's custom alert, framed by length: a short single line goes out as a one-liner, anything
    /// else in the full frame. Critical alerts open with the urgent line either way.
    /// </summary>
    public static string BuildAlertMessage(string senderName, string text, bool critical)
    {
        var body = Escape(text.Trim());
        var sb = new StringBuilder();

        if (LucianAlertRules.IsOneLiner(text))
        {
            if (critical)
                sb.AppendLine(UrgentLine);
            sb.Append(OneLine($"At the request of <b>{Escape(senderName)}</b>: {body}"));
            return sb.ToString();
        }

        if (critical)
            sb.AppendLine(UrgentLine).AppendLine();
        sb.AppendLine(OpenerLine).AppendLine();
        sb.AppendLine($"At the request of <b>{Escape(senderName)}</b>:\n");
        sb.AppendLine(body);
        return Close(sb);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static StringBuilder Open() => new StringBuilder().AppendLine(OpenerLine).AppendLine();

    private static string Close(StringBuilder sb) => sb.Append('\n').Append(Signature).ToString();

    private static string OneLine(string sentence) => $"{sentence} {SignOff}";

    private static void AppendBullets(StringBuilder sb, IEnumerable<string> names)
    {
        foreach (var name in names)
            sb.AppendLine($"  ▪ {Escape(name)}");
    }

    private static string NumberWord(int n) => n switch
    {
        2 => "two",
        3 => "three",
        4 => "four",
        5 => "five",
        _ => n.ToString()
    };

    public static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string FormatPlaytime(int minutes)
    {
        if (minutes < 60) return $"{minutes}min";
        var hours = minutes / 60.0;
        return Math.Abs(hours - (int)hours) < 0.1 ? $"{(int)hours}h" : $"{hours:F1}h";
    }
}
