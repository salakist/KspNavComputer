namespace KspNavComputer.Core.Time;

/// <summary>
/// KSP calendar utilities.
/// KSP uses a 6-hour day (21 600 s) and a 426-day year (9 203 400 s).
/// Epoch UT = 0 corresponds to Year 1, Day 1, 00:00:00.
/// </summary>
public static class KspTime
{
    public const double SecondsPerMinute = 60.0;
    public const double SecondsPerHour   = 3_600.0;
    public const double SecondsPerDay    = 21_600.0;
    public const double SecondsPerYear   = 9_203_400.0;   // 426 days × 21 600 s

    public readonly record struct KspCalendar(int Year, int Day, int Hour, int Minute, int Second);

    /// <summary>
    /// Converts a Universal Time (seconds) to KSP calendar components.
    /// Year and Day are 1-based (Year 1 Day 1 = UT 0).
    /// </summary>
    public static KspCalendar ToKspCalendar(double ut)
    {
        if (ut < 0) ut = 0;
        long total = (long)ut;

        long years   = total / (long)SecondsPerYear;
        long rem     = total % (long)SecondsPerYear;
        long days    = rem   / (long)SecondsPerDay;
        rem          = rem   % (long)SecondsPerDay;
        long hours   = rem   / (long)SecondsPerHour;
        rem          = rem   % (long)SecondsPerHour;
        long minutes = rem   / (long)SecondsPerMinute;
        long seconds = rem   % (long)SecondsPerMinute;

        return new KspCalendar(
            Year:   (int)(years  + 1),
            Day:    (int)(days   + 1),
            Hour:   (int)hours,
            Minute: (int)minutes,
            Second: (int)seconds
        );
    }

    /// <summary>
    /// Converts KSP calendar components back to Universal Time (seconds).
    /// Year and Day are 1-based.
    /// </summary>
    public static double FromKspCalendar(int year, int day, int hour, int minute, int second) =>
        (year  - 1) * SecondsPerYear   +
        (day   - 1) * SecondsPerDay    +
        hour        * SecondsPerHour   +
        minute      * SecondsPerMinute +
        second;

    /// <summary>
    /// Returns a human-readable KSP calendar string, e.g. "Y1 D74 02:15:30".
    /// </summary>
    public static string Format(double ut)
    {
        var c = ToKspCalendar(ut);
        return $"Y{c.Year} D{c.Day:D3} {c.Hour:D2}:{c.Minute:D2}:{c.Second:D2}";
    }
}
