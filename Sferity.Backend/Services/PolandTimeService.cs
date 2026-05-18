using TimeZoneConverter;

namespace Sferity.Backend.Services;

public class PolandTimeService : IPolandTimeService
{
    private static readonly TimeZoneInfo PolandTz = TZConvert.GetTimeZoneInfo("Europe/Warsaw");

    public DateTime Now() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PolandTz);

    public DateTime EndOfPreviousDayUtc(DateOnly date)
    {
        var endOfPreviousDay = date.AddDays(-1).ToDateTime(TimeOnly.MaxValue);
        return TimeZoneInfo.ConvertTimeToUtc(endOfPreviousDay, PolandTz);
    }
   
    
    public DateTime EndOfDayUtc(DateOnly date)
    {
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue);
        return TimeZoneInfo.ConvertTimeToUtc(endOfDay, PolandTz);
    }

    public bool IsExpired(DateTime expiresAtUtc) => DateTime.UtcNow > expiresAtUtc;
    
    public bool IsNotYetActive(DateTime activeFromUtc) =>
        DateTime.UtcNow < activeFromUtc;

    public TimeSpan DelayUntilMidnight()
    {
        var nowInPoland = Now();
        var midnightInPoland = nowInPoland.Date.AddDays(1);
        var midnightUtc = TimeZoneInfo.ConvertTimeToUtc(midnightInPoland, PolandTz);
        return midnightUtc - DateTime.UtcNow;
    }
}