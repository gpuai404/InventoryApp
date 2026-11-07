namespace InventoryApp.Application.Products;

/// <summary>
/// Read models returned to the presentation/API boundary. They are deliberately
/// separate from the domain entities and contain no persistence concerns.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    decimal PriceAmount,
    string CurrencyCode,
    string? Description);

public sealed record ProductBarcodeDto(
    string Code,
    string? PackagingDescription);

public sealed record ProductVariantDto(
    Guid Id,
    Guid ProductId,
    string Name,
    string? Sku,
    IReadOnlyList<ProductBarcodeDto> Barcodes);
