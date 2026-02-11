namespace FunMasters.Shared.DTOs;

[Serializable]
public class NeedRatingModel
{
    public SuggestionDto Suggestion { get; set; } = null!;
    public decimal Score { get; set; } = 10m;
    public string? Comment { get; set; }
    public bool IsSubmitting { get; set; }
    public string GetRatingLabel() => RatingUtils.GetRatingLabel((int)(Score * 10));

    public NeedRatingModel() { }
        
    public NeedRatingModel(SuggestionDto suggestion)
    {
        Suggestion = suggestion;
    }
}