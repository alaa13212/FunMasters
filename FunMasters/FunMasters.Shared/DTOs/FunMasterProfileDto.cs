using FunMasters.Shared;

namespace FunMasters.Shared.DTOs;

public class FunMasterProfileDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public CouncilStatus CouncilStatus { get; set; } = CouncilStatus.Active;
    public List<UserBadgeDto> Badges { get; set; } = [];
    public List<SuggestionDto> SuggestedGames { get; set; } = [];
    public List<UserRatingDto> ReviewedGames { get; set; } = [];
    public List<FunMasterCommentDto> Comments { get; set; } = [];

    /// <summary>The three titles this member scored highest; ties fall to whichever they judged first.</summary>
    public List<UserRatingDto> TopRatedGames { get; set; } = [];

    /// <summary>Up to three of their reviews that drew gems, richest first. Empty when they have none.</summary>
    public List<UserRatingDto> BestReviews { get; set; } = [];

    /// <summary>Gems their reviews have drawn, all time.</summary>
    public int TotalGems { get; set; }

    public CriminalRecordDto CriminalRecord { get; set; } = new();

    public List<CycleAwardDto> WriterOfTheCycleWins { get; set; } = [];

    /// <summary>What Lucian has to say about this member's record. Empty when it is in order.</summary>
    public List<string> LucianRemarks { get; set; } = [];
}

public class CriminalRecordDto
{
    /// <summary>Concluded titles they owed a verdict on and never delivered.</summary>
    public int MissedVerdicts { get; set; }

    /// <summary>Verdicts delivered with fewer than three words.</summary>
    public int WeakReviews { get; set; }

    public int Total => MissedVerdicts + WeakReviews;
}

public class CycleAwardDto
{
    public int CycleNumber { get; set; }
    public int GemCount { get; set; }
    public DateTime? EndAtUtc { get; set; }
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
