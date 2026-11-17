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

    /// <summary>
    /// Reports whether a category is referenced by at least one active product.
    /// Archived products are deliberately excluded from this catalog-consistency check.
    /// </summary>
    Task<bool> HasActiveProductsInCategoryAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken = default);
}
