using FunMasters.Data;

namespace FunMasters.Models;

public class RatingRecord(Suggestion game, RatingValue score = RatingValue.Perfect, string? comment = null)
{
    public Suggestion Game { get; init; } = game;
    public RatingValue Score { get; set; } = score;
    public string? Comment { get; set; } = comment;
        
}