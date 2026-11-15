using WarrantyIssueXE.Application.Abstractions.Time;

namespace WarrantyIssueXE.Infrastructure.Time;

/// <summary>
/// Derives the configured business date from an injected source of UTC time.
/// </summary>
internal sealed class TimeZoneBusinessClock(TimeProvider timeProvider, TimeZoneInfo timeZone)
    : IBusinessClock
{
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly TimeZoneInfo _timeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));

    public BusinessTime GetCurrent()
    {
        var utcNow = _timeProvider.GetUtcNow();
        var localTime = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
        return new BusinessTime(utcNow, DateOnly.FromDateTime(localTime.DateTime));
    }
}
