namespace WarrantyIssueXE.Application.IssuerProfiles;

/// <summary>
/// Indicates that another operation created the singleton issuer profile at the
/// same time. The caller should reload the current profile.
/// </summary>
public sealed class IssuerProfileConflictException()
    : InvalidOperationException(
        "The issuer profile was changed by another operation. Reload it and try again.");
