namespace InventoryApp.Domain.Exceptions;

/// <summary>
/// Creates exceptions for warranty-specific business rules.
/// </summary>
internal static class WarrantyExceptions
{
    public static DomainRuleViolationException ExpirationPrecedesStart(
        string propertyName) =>
        new(
            DomainErrorCodes.WarrantyPeriodInvalid,
            "Warranty expiration cannot be before its start date.",
            propertyName);
}
