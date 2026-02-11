namespace FunMasters.Shared.DTOs;

[Serializable]
public class RatingModel
{
    public UserRatingDto Rating { get; set; } = null!;
    public decimal Score { get; set; }
    public string? Comment { get; set; }
    public bool IsSubmitting { get; set; }

    public RatingModel() {}
    public RatingModel(UserRatingDto rating)
    {
        Rating = rating;
        Score = rating.DecimalScore;
        Comment = rating.Comment;
    }

    public string GetRatingLabel() => RatingUtils.GetRatingLabel((int)(Score * 10));
}