namespace WarrantyIssueXE.Application.Common;

/// <summary>
/// A stable validation failure at an application boundary. Domain validation is
/// still used for invariant rules owned by Domain.
/// </summary>
public sealed class ApplicationValidationException(string message, string propertyName)
    : ArgumentException(message, propertyName)
{
}
