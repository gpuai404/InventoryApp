using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Application.Abstractions.Persistence;

/// <summary>Persistence boundary for the single current issuer profile.</summary>
public interface IIssuerProfileStore
{
    Task<IssuerProfile?> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(IssuerProfile profile, CancellationToken cancellationToken = default);
}
