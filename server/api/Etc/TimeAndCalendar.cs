using System;

namespace api.Etc;

public static class TimeAndCalendar
{
    public static DateOnly IsoWeekStartLocal(DateTimeOffset nowUtc)
    {
        var local = TimeZoneInfo.ConvertTime(nowUtc, GetCopenhagenTz()).DateTime;
        var d = local.Date;
        int diff = (7 + (d.DayOfWeek - DayOfWeek.Monday)) % 7; 
        var monday = d.AddDays(-diff);
        return DateOnly.FromDateTime(monday);
    }

    // Returns next ISO week’s Monday.
    public static DateOnly NextIsoWeekStart(DateOnly currentMonday) => currentMonday.AddDays(7);
    
    public static bool CutoffPassed(TimeProvider tp, DateOnly weekStart)
    {
        var tz = GetCopenhagenTz();
        
        var saturday = weekStart.AddDays(5); 
        var cutoffLocal = new DateTime(
            saturday.Year, saturday.Month, saturday.Day,
            17, 0, 0, DateTimeKind.Unspecified);

        var cutoffUtc = TimeZoneInfo.ConvertTimeToUtc(cutoffLocal, tz);
        var nowUtc = tp.GetUtcNow().UtcDateTime;

        return nowUtc >= cutoffUtc;
    }
    
    private static TimeZoneInfo GetCopenhagenTz()
    {
        const string linuxId = "Europe/Copenhagen";
        const string windowsId = "Romance Standard Time";

        try { return TimeZoneInfo.FindSystemTimeZoneById(linuxId); }
        catch
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(windowsId); }
            catch { return TimeZoneInfo.Utc; } 
        }
    }
}
