namespace WarrantyIssueXE.Application.Abstractions.Time;

/// <summary>
/// Supplies the current instant and the configured local business date without
/// coupling application workflows to the system clock or a time zone.
/// </summary>
public interface IBusinessClock
{
    BusinessTime GetCurrent();
    DateTimeOffset GetUtcNow() => GetCurrent().UtcNow;
    DateOnly GetBusinessDate() => GetCurrent().BusinessDate;
}

public readonly record struct BusinessTime(DateTimeOffset UtcNow, DateOnly BusinessDate);
