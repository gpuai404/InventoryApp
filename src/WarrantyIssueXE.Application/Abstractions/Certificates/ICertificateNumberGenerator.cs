namespace WarrantyIssueXE.Application.Abstractions.Certificates;

/// <summary>
/// Generates a candidate warranty certificate number. Persistence remains the
/// final authority for uniqueness.
/// </summary>
public interface ICertificateNumberGenerator
{
    string Generate();
}
