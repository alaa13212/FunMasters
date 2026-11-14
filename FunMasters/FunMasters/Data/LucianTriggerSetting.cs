using System.ComponentModel.DataAnnotations;

namespace FunMasters.Data;

/// <summary>
/// An admin's switch for one of Lucian's triggers. A trigger with no row is on, so new triggers
/// speak from the day they ship and only a deliberate choice silences one.
/// </summary>
public class LucianTriggerSetting
{
    /// <summary>One of the keys in <see cref="Shared.LucianTriggers"/>.</summary>
    [Key]
    [StringLength(64)]
    public string Key { get; set; } = null!;

    public bool Enabled { get; set; } = true;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
