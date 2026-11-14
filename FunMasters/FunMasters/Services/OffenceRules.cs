using FunMasters.Data;
using FunMasters.Shared;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

/// <summary>
/// The Council's definitions of a delinquent verdict, in one place.
/// Both the reminder job and the criminal record on a member's profile read from here,
/// so what Lucian shames a member for and what their record shows can never drift apart.
/// </summary>
public static class OffenceRules
{
    public const int MinReviewWords = ReviewRules.MinReviewWords;

    /// <summary>
    /// How much of a verdict is examined for substance. Comments run to 50,000 characters and the
    /// member roll only fetches this opening slice, so every surface must judge the same slice or
    /// the same member ends up with two different counts.
    /// </summary>
    public const int CommentSampleLength = 512;

    /// <summary>
    /// A verdict carries substance when it has at least <see cref="MinReviewWords"/> words.
    /// Pass the already-truncated sample; use <see cref="IsVerdictSubstantial"/> for a full comment.
    /// </summary>
    public static bool IsCommentSubstantial(string? comment)
    {
        // Counted by the shared rule, so the review form's live counter warns at exactly the point
        // this record starts counting.
        return ReviewRules.CountWords(comment) >= MinReviewWords;
    }

    /// <summary>
    /// As <see cref="IsCommentSubstantial"/>, but takes a full comment and judges only the opening
    /// slice — so a profile and the member roll agree by construction.
    /// </summary>
    public static bool IsVerdictSubstantial(string? comment) =>
        IsCommentSubstantial(
            comment is { Length: > CommentSampleLength } long_ ? long_[..CommentSampleLength] : comment);

    /// <summary>
    /// Concluded titles that can be owed a verdict at all. A title with neither an active nor a
    /// finished date never went before the Council — an admin can create one by hand — and has no
    /// cutoff to judge registration against, so it is excluded rather than allowed to throw.
    /// </summary>
    public static IQueryable<Suggestion> JudgeableFinishedGames(ApplicationDbContext db) =>
        db.Suggestions.Where(s => s.Status == SuggestionStatus.Finished
                                  && (s.ActiveAtUtc != null || s.FinishedAtUtc != null));

    /// <summary>
    /// The moment a member had to have joined by to owe a verdict on this game —
    /// when it went before the Council, or failing that when it concluded.
    /// </summary>
    public static DateTime Cutoff(Suggestion suggestion) =>
        suggestion.ActiveAtUtc ?? suggestion.FinishedAtUtc!.Value;

    /// <summary>
    /// A member owes a verdict on a concluded title when their standing obliges them to review and
    /// they had joined before it went before the Council.
    ///
    /// Deliberately does NOT require a seat in the rotation: declining to put a title forward does
    /// not excuse you from judging everyone else's. A member with CycleOrder 0 still owes verdicts.
    /// </summary>
    public static bool OwesVerdict(ApplicationUser user, DateTime gameCutoff) =>
        CouncilStatusRoles.MustReview.Contains(user.CouncilStatus)
        && user.RegistrationDateUtc <= gameCutoff;
}
