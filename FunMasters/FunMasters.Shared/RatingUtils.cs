namespace FunMasters.Shared;

public static class RatingUtils
{
    public static string GetRatingLabel(int score) => score switch
    {
        100 => "Prefect",
        >= 90 => "Superb",
        >= 80 => "Great",
        >= 70 => "Good",
        >= 60 => "Decent",
        >= 50 => "Average",
        >= 40 => "Poor",
        >= 30 => "Bad",
        >= 20 => "Terrible",
        >= 10 => "Abysmal",
        _ => "WTF"
    };
}
