namespace WarrantyIssueXE.Domain.Exceptions;

/// <summary>
/// Creates exceptions for warranty-specific business rules.
/// </summary>
internal static class WarrantyExceptions
{
    public static DomainRuleViolationException ExpirationDoesNotMatchTerm(
        string propertyName) =>
        new(
            DomainErrorCodes.WarrantyPeriodInvalid,
            "Warranty expiration must be the inclusive end date derived from its coverage term.",
            propertyName);

    public static DomainRuleViolationException ExpirationPrecedesStart(string propertyName) =>
        new(
            DomainErrorCodes.WarrantyPeriodInvalid,
            "Warranty expiration cannot precede its start date.",
            propertyName);

    public static DomainRuleViolationException BusinessDateIsInFuture(string propertyName) =>
        new(
            DomainErrorCodes.WarrantyBusinessDateInvalid,
            "The sale or repair date cannot be later than the issuance business date.",
            propertyName);

    public static DomainRuleViolationException DetailsInvalid(
        string propertyName,
        string message) =>
        new(
            DomainErrorCodes.WarrantyDetailsInvalid,
            message,
            propertyName);

    public static DomainRuleViolationException CoverageInvalid(
        string propertyName,
        string message) =>
        new(
            DomainErrorCodes.WarrantyCoverageInvalid,
            message,
            propertyName);

    public static DomainValidationException TimestampRequired(string propertyName) =>
        new(
            DomainErrorCodes.WarrantyTimestampRequired,
            "A warranty timestamp is required.",
            propertyName);
}
