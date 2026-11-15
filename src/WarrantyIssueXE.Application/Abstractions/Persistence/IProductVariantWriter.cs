using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;

namespace WarrantyIssueXE.Application.Abstractions.Persistence;

/// <summary>
/// Write port for product variants.
/// </summary>
public interface IProductVariantWriter
{
    Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductVariant variant, CancellationToken cancellationToken = default);

    Task ArchiveAsync(ProductVariantId id, CancellationToken cancellationToken = default);
}
