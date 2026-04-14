namespace FunMasters.Shared.DTOs;

public class FunMasterProfileDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string CouncilStatus { get; set; } = "Active";
    public List<UserBadgeDto> Badges { get; set; } = [];
    public List<SuggestionDto> SuggestedGames { get; set; } = [];
    public List<UserRatingDto> ReviewedGames { get; set; } = [];
    public List<FunMasterCommentDto> Comments { get; set; } = [];
}

public class FunMasterCommentDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorUserName { get; set; } = null!;
    public string? AuthorAvatarUrl { get; set; }
    public List<UserBadgeDto> AuthorBadges { get; set; } = [];
    public string Text { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateFunMasterCommentRequest
{
    public string Text { get; set; } = null!;
}
