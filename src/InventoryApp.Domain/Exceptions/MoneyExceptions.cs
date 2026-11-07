namespace InventoryApp.Domain.Exceptions;

/// <summary>
/// Creates exceptions for money-value invariants.
/// </summary>
internal static class MoneyExceptions
{
    public static DomainValidationException NegativeAmount(string propertyName) =>
        new(
            DomainErrorCodes.NegativeMoneyAmount,
            "Money amount cannot be negative.",
            propertyName);

    public static DomainValidationException InvalidCurrencyCode(string propertyName) =>
        new(
            DomainErrorCodes.InvalidCurrencyCode,
            "Currency code must contain three ASCII letters.",
            propertyName);
}
