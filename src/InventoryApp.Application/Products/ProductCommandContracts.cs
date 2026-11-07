namespace InventoryApp.Application.Products;

/// <summary>
/// Input models for product and product-variant commands. These are boundary
/// models, so UI/API callers do not need to construct domain value objects.
/// </summary>
public sealed record CreateProductRequest(
    Guid CategoryId,
    string Name,
    decimal PriceAmount,
    string CurrencyCode,
    string? Description = null);

public sealed record UpdateProductRequest(
    Guid CategoryId,
    string Name,
    decimal PriceAmount,
    string CurrencyCode,
    string? Description = null);

public sealed record ProductBarcodeInput(
    string Code,
    string? PackagingDescription = null);

public sealed record CreateProductVariantRequest(
    Guid ProductId,
    string Name,
    string? Sku = null,
    IReadOnlyCollection<ProductBarcodeInput>? Barcodes = null);

public sealed record UpdateProductVariantRequest(
    string Name,
    string? Sku,
    IReadOnlyCollection<ProductBarcodeInput> Barcodes);

public sealed record AddProductVariantBarcodeRequest(
    string Code,
    string? PackagingDescription = null);

public sealed record RemoveProductVariantBarcodeRequest(string Code);
