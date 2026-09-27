using FunMasters.Shared;

namespace FunMasters.Shared.DTOs;

[Serializable]
public class ReviewFormModel
{
    private decimal _score = 10m;

    /// <summary>
    /// Assigning a score marks it as chosen, so loading an existing verdict for editing counts as
    /// assigned while a fresh form does not — the field initialiser bypasses this setter.
    /// </summary>
    public decimal Score
    {
        get => _score;
        set
        {
            _score = value;
            HasScore = true;
        }
    }

    /// <summary>False until the member has actually picked a score; the dial reads "--" until then.</summary>
    public bool HasScore { get; set; }

    public string? Comment { get; set; }

    /// <summary>Lifetime playtime, in hours.</summary>
    public decimal? PlaytimeHours { get; set; }

    /// <summary>
    /// Playtime inside the title's active window, in hours. Only meaningful while the title is
    /// before the Council or at the moment it concluded, so forms that have nothing to say about
    /// that window leave <see cref="ShowPlaytime2Weeks"/> off and never show the field.
    /// </summary>
    public decimal? Playtime2WeeksHours { get; set; }

    /// <summary>Whether this form offers the active-window playtime field alongside the lifetime one.</summary>
    public bool ShowPlaytime2Weeks { get; set; }

    public bool IsSubmitting { get; set; }

    public string GetRatingLabel() => RatingUtils.GetRatingLabel((int)(Score * 10));

    /// <summary>
    /// Whether a title's active window - the stretch the Council gave it to be played - has begun.
    /// Only then can anyone say how much playtime fell inside it, so only then is the field offered.
    /// </summary>
    public static bool HasActiveWindow(SuggestionStatus status) =>
        status is SuggestionStatus.Active or SuggestionStatus.Finished;
}
