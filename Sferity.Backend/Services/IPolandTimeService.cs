namespace Sferity.Backend.Services;

public interface IPolandTimeService
{
    DateTime Now();
    DateTime EndOfPreviousDayUtc(DateOnly date); 
    DateTime EndOfDayUtc(DateOnly date);
    bool IsExpired(DateTime expiresAtUtc);
    bool IsNotYetActive(DateTime activeFromUtc);
    TimeSpan DelayUntilMidnight();
}