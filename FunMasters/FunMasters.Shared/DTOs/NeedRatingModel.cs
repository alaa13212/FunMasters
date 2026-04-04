namespace FunMasters.Shared.DTOs;

[Serializable]
public class NeedRatingModel
{
    public SuggestionDto Suggestion { get; set; } = null!;
    public ReviewFormModel Form { get; set; } = new();

    public NeedRatingModel() { }

    public NeedRatingModel(SuggestionDto suggestion)
    {
        Suggestion = suggestion;
    }
}
