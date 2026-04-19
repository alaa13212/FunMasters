using FunMasters.Data;
using FunMasters.Shared;

namespace FunMasters.Services;

public class LucianGalade(TelegramService telegram, ApplicationDbContext db, ILogger<LucianGalade> logger)
{
    private static readonly TimeSpan MorningHourUtc = TimeSpan.FromHours(6); // 6 AM UTC = 9 AM UTC+3

    public async Task QueueForMorningAsync(string message)
    {
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

    private async Task SendSafeAsync(string message)
    {
        try
        {
            await telegram.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Telegram notification");
        }
    }

    public async Task SendRatingReminderAsync(
        string gameTitle,
        int unratedCount,
        int daysSinceFinish,
        IEnumerable<string> unratedMemberNames,
        bool includeShortCommentMembers,
        bool isRepeat = false)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");

        if (!includeShortCommentMembers)
        {
            sb.AppendLine($"It has been <b>{daysSinceFinish} days</b> since the Council concluded its deliberation " +
                          $"of <b>{Escape(gameTitle)}</b>.");
            sb.AppendLine($"<b>{unratedCount}</b> member(s) have yet to deliver their verdict. " +
                          "The Council grows impatient.\n");

            sb.AppendLine("Those yet to render judgment:");
            foreach (var name in unratedMemberNames)
                sb.AppendLine($"  ▪ {Escape(name)}");
        }
        else
        {
            sb.AppendLine($"A gentle reminder regarding <b>{Escape(gameTitle)}</b>: " +
                          "the Council notes that some verdicts, while delivered, lack the substance befitting a Fun Master's review. " +
                          "A verdict with fewer than three words hardly does justice to the deliberation.\n");
            sb.AppendLine("The Council encourages these members to elaborate:");
            foreach (var name in unratedMemberNames)
                sb.AppendLine($"  ▪ {Escape(name)}");
        }

        if (isRepeat)
        {
            sb.AppendLine();
            sb.AppendLine("The Council would hate for Anton Rayne to take an interest in this matter.");
        }

        sb.AppendLine("\nThe Council awaits.");
        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        await SendSafeAsync(sb.ToString());
    }

    public async Task SendAllRatingsInAsync(
        string gameTitle,
        decimal averageScore,
        string ratingLabel,
        int ratingsCount)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");

        sb.AppendLine($"The Council has reached consensus on <b>{Escape(gameTitle)}</b>.\n");
        sb.AppendLine($"<b>{averageScore:F1}/10</b> — <i>{Escape(ratingLabel)}</i>");
        sb.AppendLine($"{ratingsCount} verdict(s) delivered.");

        sb.AppendLine("\nThe record stands.");
        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        await SendSafeAsync(sb.ToString());
    }

    public async Task SendFinishEarlyAsync(
        string gameTitle,
        int daysCut,
        DateTime newEndDate)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");

        sb.AppendLine($"By order of the Chair, the deliberation period for <b>{Escape(gameTitle)}</b> " +
                      $"has been shortened by <b>{daysCut} day(s)</b>.\n");
        sb.AppendLine($"The new conclusion date is <b>{newEndDate:dd MMM yyyy}</b>. " +
                      "Final playtimes will be captured, and the next title shall commence shortly.");
        sb.AppendLine("\nThe Council adjusts its schedule accordingly.");

        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        await SendSafeAsync(sb.ToString());
    }

    public async Task SendWeeklyDigestAsync(
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
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");
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
        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        await SendSafeAsync(sb.ToString());
    }

    public async Task SendPlaytimeShamingAsync(
        string gameTitle,
        int daysRemaining,
        IEnumerable<string> absentMembers,
        bool isRepeat = false)
    {
        var members = absentMembers.ToList();
        if (members.Count == 0) return;

        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");

        if (members.Count == 1)
        {
            sb.AppendLine($"{Escape(members[0])}, the Council notes with... interest... " +
                          $"that you have yet to begin <b>{Escape(gameTitle)}</b>.");
        }
        else
        {
            sb.AppendLine("The Council notes with... interest... that the following members have " +
                          $"yet to begin <b>{Escape(gameTitle)}</b>:");
            foreach (var name in members)
                sb.AppendLine($"  ▪ {Escape(name)}");
        }

        sb.AppendLine($"\nOnly <b>{daysRemaining} day(s)</b> remain in the deliberation period.");

        if (isRepeat)
            sb.AppendLine("\nThe Council would hate for Anton Rayne to take an interest in this matter.");

        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        await SendSafeAsync(sb.ToString());
    }

    public async Task SendPlaytimeMilestoneAsync(
        string gameTitle,
        string userName,
        string milestone,
        int totalMinutes)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");

        sb.AppendLine($"The Council observes that {Escape(userName)} has reached the <b>{Escape(milestone)}</b> milestone " +
                      $"in <b>{Escape(gameTitle)}</b> — with a total of {FormatPlaytime(totalMinutes)} dedicated.");

        sb.AppendLine("\nDuly noted.");
        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        await SendSafeAsync(sb.ToString());
    }

    public async Task SendNewMemberAsync(string userName)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");

        sb.AppendLine($"The Council welcomes its newest member, <b>{Escape(userName)}</b>. " +
                      "May their suggestions be ever inspired and their playtime plentiful.");

        sb.AppendLine("\nA seat has been prepared.");
        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        await SendSafeAsync(sb.ToString());
    }

    public async Task SendNewSuggestionAsync(string userName, string gameTitle)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");

        sb.AppendLine($"{Escape(userName)} has submitted <b>{Escape(gameTitle)}</b> for the Council's consideration.");

        sb.AppendLine("\nThe docket has been updated.");
        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        await SendSafeAsync(sb.ToString());
    }

    public async Task SendSuggestionEditedAsync(string userName, string newTitle)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");

        sb.AppendLine($"{Escape(userName)} has amended their proposal: <b>{Escape(newTitle)}</b>.");

        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        await SendSafeAsync(sb.ToString());
    }

    public async Task SendSuggestionDeletedAsync(string userName)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");

        sb.AppendLine($"{Escape(userName)} has withdrawn a proposal from the Council's consideration.");

        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        await SendSafeAsync(sb.ToString());
    }

    public async Task SendPriceBackUpAsync(string gameTitle, string originalPrice)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");

        sb.AppendLine($"The discount on <b>{Escape(gameTitle)}</b> has ended. " +
                      $"The title now returns to its standard price of {Escape(originalPrice)}.");

        sb.AppendLine("\nThe Council may wish they had acted sooner.");
        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        // await SendSafeAsync(sb.ToString());
        await telegram.SendMessageAsync(sb.ToString());
    }

    public async Task SendGameFreeAsync(string gameTitle, string steamLink)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");

        sb.AppendLine($"A rare occurrence: <b>{Escape(gameTitle)}</b> — a title currently awaiting " +
                      "the Council's deliberation — is now <b>FREE</b> on Steam.\n");

        if (!string.IsNullOrWhiteSpace(steamLink))
            sb.AppendLine(steamLink);

        sb.AppendLine("\nThe Council would do well to act swiftly.");
        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        await SendSafeAsync(sb.ToString());
    }

    public async Task SendQuarterlyDigestAsync(
        string topRatedTitle,
        decimal topRatedScore,
        string lowestRatedTitle,
        decimal lowestRatedScore,
        string mostPlayedTitle,
        int mostPlayedMinutes,
        string mostControversialTitle,
        string? mostDivisiveTitle)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");
        sb.AppendLine("As the season turns, the Council's records have been reviewed. A summary of distinction follows.\n");

        sb.AppendLine($"<b>Highest Acclaim:</b> {Escape(topRatedTitle)} — {topRatedScore:F1}/10");
        sb.AppendLine($"<b>Lowest Regard:</b> {Escape(lowestRatedTitle)} — {lowestRatedScore:F1}/10");
        sb.AppendLine($"<b>Most Deliberated:</b> {Escape(mostPlayedTitle)} — {FormatPlaytime(mostPlayedMinutes)} collectively invested");
        sb.AppendLine($"<b>Most Controversial:</b> {Escape(mostControversialTitle)} — the widest divergence of opinion");

        if (mostDivisiveTitle != null)
            sb.AppendLine($"<b>Most Divisive:</b> {Escape(mostDivisiveTitle)} — love or hate, no middle ground");

        sb.AppendLine("\nThe Council's legacy endures.");
        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        await SendSafeAsync(sb.ToString());
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string FormatPlaytime(int minutes)
    {
        if (minutes < 60) return $"{minutes}min";
        var hours = minutes / 60.0;
        return Math.Abs(hours - (int)hours) < 0.1 ? $"{(int)hours}h" : $"{hours:F1}h";
    }

    public static string BuildGameRotationMessage(
        string outgoingTitle,
        IEnumerable<(string UserName, int PlaytimeMinutes)> outgoingPlaytimes,
        string incomingTitle,
        string incomingSuggester,
        string? incomingSteamLink)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Esteemed members of the <b>Council of Fun Masters</b>,\n");
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
        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");

        return sb.ToString();
    }

    public static string BuildDiscountMessage(List<(string Title, int AppId, SteamPriceInfo Price)> games)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Esteemed members of <b>The Council of Fun Masters</b>,\n");

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
        sb.Append("\n<i>— Lucian Galade, Chief of Staff</i>");
        return sb.ToString();
    }
}
