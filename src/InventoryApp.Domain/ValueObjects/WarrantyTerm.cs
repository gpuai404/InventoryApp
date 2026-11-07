using InventoryApp.Domain.Exceptions;

namespace InventoryApp.Domain.ValueObjects;

/// <summary>
/// The calendar-month duration a service grants for a warranty.
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
    /// Returns the final active day of the term. A warranty starting on
    /// 1 January for twelve months is therefore active through 31 December.
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
