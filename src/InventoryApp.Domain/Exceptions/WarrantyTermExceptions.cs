namespace InventoryApp.Domain.Exceptions;

/// <summary>
/// Creates exceptions for warranty-term invariants and calculations.
/// </summary>
internal static class WarrantyTermExceptions
{
    public static DomainValidationException MonthsMustBePositive(string propertyName) =>
        new(
            DomainErrorCodes.WarrantyTermMonthsInvalid,
            "A warranty term must be at least one month.",
            propertyName);

    public static DomainRuleViolationException CannotBeApplied(
        string propertyName,
        Exception innerException) =>
        new(
            DomainErrorCodes.WarrantyTermCannotBeApplied,
            "The warranty term cannot be applied to the start date.",
            propertyName,
            innerException);
}
