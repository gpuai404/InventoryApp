namespace InventoryApp.Domain.Exceptions;

/// <summary>
/// Creates validation exceptions for generic domain input constraints.
/// </summary>
internal static class DomainValidationExceptions
{
    public static DomainValidationException RequiredValue(string propertyName) =>
        new(
            DomainErrorCodes.RequiredValue,
            $"The value for '{propertyName}' is required.",
            propertyName);

    public static DomainValidationException EmptyIdentifier(string propertyName) =>
        new(
            DomainErrorCodes.EmptyIdentifier,
            $"The identifier for '{propertyName}' cannot be empty.",
            propertyName);

    public static DomainValidationException TextExceedsMaximumLength(
        string propertyName,
        int maximumLength) =>
        new(
            DomainErrorCodes.TextTooLong,
            $"The value for '{propertyName}' cannot exceed {maximumLength} characters.",
            propertyName);
}
