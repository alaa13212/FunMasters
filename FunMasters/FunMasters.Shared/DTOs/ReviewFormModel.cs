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
    public decimal? PlaytimeHours { get; set; }
    public bool IsSubmitting { get; set; }

    public string GetRatingLabel() => RatingUtils.GetRatingLabel((int)(Score * 10));
}
