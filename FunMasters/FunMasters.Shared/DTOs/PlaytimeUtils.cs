namespace FunMasters.Shared.DTOs;

public static class PlaytimeUtils
{
    public static string FormatMinutes(int? minutes)
    {
        if (!minutes.HasValue) return "N/A";
        if (minutes.Value < 60) return $"{minutes.Value}m";
        return $"{minutes.Value / 60.0:0.#}h";
    }
}
