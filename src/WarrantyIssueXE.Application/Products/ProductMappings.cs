using WarrantyIssueXE.Domain.Entities;

namespace WarrantyIssueXE.Application.Products;

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
}
