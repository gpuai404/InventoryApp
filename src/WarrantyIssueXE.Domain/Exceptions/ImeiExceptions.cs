namespace WarrantyIssueXE.Domain.Exceptions;

internal static class ImeiExceptions
{
    public static DomainValidationException Invalid(string propertyName) =>
        new(
            DomainErrorCodes.InvalidImei,
            "IMEI must contain exactly 15 digits and have a valid check digit.",
            propertyName);
}
