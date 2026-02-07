using FunMasters.Data;
using FunMasters.Services;

namespace FunMasters.Models;

public class RatingRecord(Suggestion game, decimal score = 10m, string? comment = null)
{
    public Suggestion Game { get; init; } = game;
    public decimal Score { get; set; } = score;
    public string? Comment { get; set; } = comment;
    
    public string GetRatingLabel() => RatingUtils.GetRatingLabel((int)(Score * 10));

}