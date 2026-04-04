namespace FunMasters.Shared.DTOs;

[Serializable]
public class RatingModel
{
    public UserRatingDto Rating { get; set; } = null!;
    public ReviewFormModel Form { get; set; } = new();

    public RatingModel() { }

    public RatingModel(UserRatingDto rating)
    {
        Rating = rating;
        Form = new ReviewFormModel
        {
            Score = rating.DecimalScore,
            Comment = rating.Comment,
            PlaytimeHours = rating.PlaytimeForeverMinutes.HasValue
                ? Math.Round(rating.PlaytimeForeverMinutes.Value / 60m, 1)
                : null
        };
    }
}
