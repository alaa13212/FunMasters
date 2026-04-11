namespace FunMasters.Shared;

public static class FunMastersTime
{
    public static readonly TimeSpan UtcPlus3 = TimeSpan.FromHours(3);

    public static readonly TimeSpan GamePlayPeriod = TimeSpan.FromDays(14);
    public static readonly TimeSpan ShortPlayPeriod = TimeSpan.FromDays(7);

    public static DateTime UtcNow => DateTime.UtcNow;

    public static DateTime MidnightUtc3AsUtc(DateTime date)
    {
        return DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified) - UtcPlus3;
    }

    public static DateTime NextMidnightUtc3()
    {
        var now = UtcNow;
        var localNow = now + UtcPlus3;
        var nextLocalMidnight = localNow.Date.AddDays(1);
        return DateTime.SpecifyKind(nextLocalMidnight, DateTimeKind.Unspecified) - UtcPlus3;
    }

    public static DateTime CurrentOrNextMidnightUtc3()
    {
        var now = UtcNow;
        var localNow = now + UtcPlus3;
        if (localNow.TimeOfDay == TimeSpan.Zero)
            return DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified) - UtcPlus3;
        var nextLocalMidnight = localNow.Date.AddDays(1);
        return DateTime.SpecifyKind(nextLocalMidnight, DateTimeKind.Unspecified) - UtcPlus3;
    }
}
