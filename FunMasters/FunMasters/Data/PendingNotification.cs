using System.ComponentModel.DataAnnotations;

namespace FunMasters.Data;

public class PendingNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(10000)]
    public string Message { get; set; } = null!;

    public DateTime SendAfterUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
