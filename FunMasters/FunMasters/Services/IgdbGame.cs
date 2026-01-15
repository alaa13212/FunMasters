namespace FunMasters.Services;

public class IgdbGame
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public IgdbLink cover { get; set; }
    public List<IgdbLink> websites { get; set; }
}

public class IgdbLink
{
    public int id { get; set; }
    public string url { get; set; }
}