namespace FunMasters.Data;

public sealed class FunMasterComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TargetUserId { get; set; }
    public Guid AuthorId { get; set; }
    public string Text { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    public ApplicationUser TargetUser { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
}
