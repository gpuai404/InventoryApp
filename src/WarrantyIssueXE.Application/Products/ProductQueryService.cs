using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Domain.Identifiers;

namespace WarrantyIssueXE.Application.Products;

/// <summary>
/// Coordinates product catalog reads with no write dependency.
/// </summary>
public sealed class ProductQueryService(IProductReader productReader)
{
    private readonly IProductReader _productReader =
        productReader ?? throw new ArgumentNullException(nameof(productReader));

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
}
