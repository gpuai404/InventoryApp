using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Application.Products;

/// <summary>
/// Coordinates product read use cases. It has no write dependency.
/// </summary>
public sealed class ProductQueryService
{
    private readonly IProductReader _productReader;
    private readonly IProductVariantReader _variantReader;

    public ProductQueryService(
        IProductReader productReader,
        IProductVariantReader variantReader)
    {
        _productReader = productReader ?? throw new ArgumentNullException(nameof(productReader));
        _variantReader = variantReader ?? throw new ArgumentNullException(nameof(variantReader));
    }

    public async Task<ProductDto?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _productReader.GetByIdAsync(
            new ProductId(productId),
            cancellationToken);

        return product?.ToDto();
    }

    public async Task<IReadOnlyList<ProductDto>> ListProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await _productReader.ListAsync(cancellationToken);
        return products.Select(product => product.ToDto()).ToArray();
    }

    public async Task<ProductVariantDto?> GetProductVariantAsync(
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        var variant = await _variantReader.GetByIdAsync(
            new ProductVariantId(variantId),
            cancellationToken);

        return variant?.ToDto();
    }

    public async Task<IReadOnlyList<ProductVariantDto>> ListProductVariantsAsync(
        CancellationToken cancellationToken = default)
    {
        var variants = await _variantReader.ListAsync(cancellationToken);
        return variants.Select(variant => variant.ToDto()).ToArray();
    }

    public async Task<ProductVariantDto?> FindVariantByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var productBarcode = new ProductBarcode(barcode);
        var variant = await _variantReader.FindByBarcodeAsync(
            productBarcode,
            cancellationToken);

        return variant?.ToDto();
    }
}
