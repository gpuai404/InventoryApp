using WarrantyIssueXE.Application.Abstractions.Certificates;

namespace WarrantyIssueXE.Infrastructure.Certificates;

/// <summary>
/// Generates collision-resistant certificate numbers without requiring a
/// network or a central sequence.
/// </summary>
internal sealed class GuidCertificateNumberGenerator : ICertificateNumberGenerator
{
    public string Generate() => $"WR-{Guid.NewGuid():N}".ToUpperInvariant();
}
