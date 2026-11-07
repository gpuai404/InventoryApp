using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Products;

internal static class ProductMappings
{
    public static ProductDto ToDto(this Product product) =>
        new(
            product.Id.Value,
            product.CategoryId.Value,
            product.Name,
            product.Price.Amount,
            product.Price.CurrencyCode,
            product.Description);

    public static ProductVariantDto ToDto(this ProductVariant variant) =>
        new(
            variant.Id.Value,
            variant.ProductId.Value,
            variant.Name,
            variant.Sku,
            variant.Barcodes
                .Select(barcode => new ProductBarcodeDto(
                    barcode.Code,
                    barcode.PackagingDescription))
                .ToArray());
}
