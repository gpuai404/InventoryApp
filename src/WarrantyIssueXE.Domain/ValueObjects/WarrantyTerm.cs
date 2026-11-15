using WarrantyIssueXE.Domain.Exceptions;

namespace WarrantyIssueXE.Domain.ValueObjects;

/// <summary>
/// The calendar-month duration selected by a warranty plan or explicit coverage.
/// </summary>
public sealed record WarrantyTerm
{
    public WarrantyTerm(int months)
    {
        if (months <= 0)
        {
            throw WarrantyTermExceptions.MonthsMustBePositive(nameof(months));
        }

        Months = months;
    }

    public int Months { get; }

    /// <summary>
    /// Returns the final active day of the term by adding the calendar months
    /// with <see cref="DateOnly.AddMonths(int)"/> and subtracting one day. A
    /// warranty starting on 1 January for twelve months is active through
    /// 31 December. At month end this deliberately preserves DateOnly's clamp
    /// behavior: a one-month term starting 31 January 2026 expires 27 February.
    /// </summary>
    public DateOnly ExpiresOn(DateOnly startsOn)
    {
        try
        {
            return startsOn.AddMonths(Months).AddDays(-1);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw WarrantyTermExceptions.CannotBeApplied(nameof(startsOn), exception);
        }
    }
}
