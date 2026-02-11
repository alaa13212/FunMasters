namespace FunMasters.Shared.DTOs;

public class SuggestionDetailDto
{
    public SuggestionDto Suggestion { get; set; } = null!;
    public List<RatingDto> Ratings { get; set; } = [];
}
