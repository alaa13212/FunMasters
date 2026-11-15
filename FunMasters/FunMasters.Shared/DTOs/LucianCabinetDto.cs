namespace FunMasters.Shared.DTOs;

public class LucianCabinetDto
{
    public List<LucianTriggerDto> Triggers { get; set; } = [];

    /// <summary>
    /// Whether the viewer may send alerts through Lucian. Members the Council has cast out
    /// (excommunicated or executed) may read the Cabinet but not speak through it.
    /// </summary>
    public bool CanSendAlert { get; set; }
}

public class LucianTriggerDto
{
    public string Key { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string WhenItFires { get; set; } = null!;
    public LucianForm Form { get; set; }
    public bool Enabled { get; set; }

    /// <summary>
    /// An example of the message, built by the same code that sends the real one, from fixed sample
    /// data. Telegram HTML (b, i, s) with line breaks as newlines.
    /// </summary>
    public string SampleHtml { get; set; } = "";
}

public class SetLucianTriggerRequest
{
    public bool Enabled { get; set; }
}
