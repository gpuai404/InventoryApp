using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;

namespace WarrantyIssueXE.Application.Abstractions.Persistence;

/// <summary>
/// Write port for products. Infrastructure implements this port for SQLite or
/// another persistence mechanism.
/// </summary>
public interface IProductWriter
{
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    Task ArchiveAsync(ProductId id, CancellationToken cancellationToken = default);
}
