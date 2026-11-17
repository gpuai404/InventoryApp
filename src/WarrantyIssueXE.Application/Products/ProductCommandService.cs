using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.Common;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Application.Products;

/// <summary>
/// Coordinates product catalog commands. Product owns all catalog state;
/// individual device identification belongs to warranty issuance.
/// </summary>
public sealed class ProductCommandService
{
    private readonly ICategoryReader _categoryReader;
    private readonly IProductReader _productReader;
    private readonly IProductWriter _productWriter;
    private readonly IUnitOfWork _unitOfWork;

    public ProductCommandService(
        ICategoryReader categoryReader,
        IProductReader productReader,
        IProductWriter productWriter,
        IUnitOfWork unitOfWork)
    {
        _categoryReader = categoryReader ?? throw new ArgumentNullException(nameof(categoryReader));
        _productReader = productReader ?? throw new ArgumentNullException(nameof(productReader));
        _productWriter = productWriter ?? throw new ArgumentNullException(nameof(productWriter));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var categoryId = new CategoryId(request.CategoryId);
        await _categoryReader
            .GetByIdAsync(categoryId, cancellationToken)
            .RequireFoundAsync(categoryId.Value);

        var product = new Product(
            categoryId,
            request.Name,
            new Money(request.PriceAmount, request.CurrencyCode),
            request.Description);

        await _productWriter.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }

    public async Task<ProductDto> UpdateAsync(
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var typedProductId = new ProductId(productId);
        var product = await _productReader
            .GetByIdAsync(typedProductId, cancellationToken)
            .RequireFoundAsync(typedProductId.Value);
        var categoryId = new CategoryId(request.CategoryId);
        await _categoryReader
            .GetByIdAsync(categoryId, cancellationToken)
            .RequireFoundAsync(categoryId.Value);

        var revised = product.Revise(
            categoryId,
            request.Name,
            new Money(request.PriceAmount, request.CurrencyCode),
            request.Description);

        await _productWriter.UpdateAsync(revised, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return revised.ToDto();
    }

    public async Task ArchiveAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var typedProductId = new ProductId(productId);
        await _productReader
            .GetByIdAsync(typedProductId, cancellationToken)
            .RequireFoundAsync(typedProductId.Value);

        await _productWriter.ArchiveAsync(typedProductId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
