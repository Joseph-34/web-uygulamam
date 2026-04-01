namespace kazandakazan.Infrastructure;

public static class TurkeyTime
{
    public static DateOnly TodayFromUtc(DateTime utcNow)
    {
        var id = OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul";
        var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
        var utc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        return DateOnly.FromDateTime(local);
    }
}
