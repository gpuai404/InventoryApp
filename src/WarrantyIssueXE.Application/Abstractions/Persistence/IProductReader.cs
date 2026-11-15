using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;

namespace WarrantyIssueXE.Application.Abstractions.Persistence;

/// <summary>
/// Read port for products. Query consumers do not receive write operations.
/// </summary>
public interface IProductReader
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default);
}
