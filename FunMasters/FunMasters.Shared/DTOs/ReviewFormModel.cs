namespace FunMasters.Shared.DTOs;

[Serializable]
public class ReviewFormModel
{
    public decimal Score { get; set; } = 10m;
    public string? Comment { get; set; }
    public decimal? PlaytimeHours { get; set; }
    public bool IsSubmitting { get; set; }

    public string GetRatingLabel() => RatingUtils.GetRatingLabel((int)(Score * 10));
}
