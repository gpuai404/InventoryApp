using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Application.Abstractions.Persistence;

/// <summary>
/// Read port for product variants.
/// </summary>
public interface IProductVariantReader : IEntityReader<ProductVariant, ProductVariantId>
{
    Task<ProductVariant?> FindByBarcodeAsync(
        ProductBarcode barcode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the barcode registry, including archived variants. Barcodes remain
    /// reserved after archival because the database enforces global uniqueness.
    /// </summary>
    Task<bool> IsBarcodeReservedAsync(
        ProductBarcode barcode,
        ProductVariantId? excludingVariantId = null,
        CancellationToken cancellationToken = default);
}
