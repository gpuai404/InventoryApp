using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Application.Abstractions.Persistence;

/// <summary>
/// Read port for product variants.
/// </summary>
public interface IProductVariantReader
{
    Task<ProductVariant?> GetByIdAsync(
        ProductVariantId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductVariant>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductVariant>> ListByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default);

    Task<ProductVariant?> FindByBarcodeAsync(
        ProductBarcode barcode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the normalized codes already reserved by another variant. The
    /// lookup includes archived variants because barcode ownership remains
    /// globally unique.
    /// </summary>
    Task<IReadOnlySet<string>> FindReservedBarcodeCodesAsync(
        IReadOnlyCollection<string> barcodeCodes,
        ProductVariantId? excludingVariantId = null,
        CancellationToken cancellationToken = default);
}
