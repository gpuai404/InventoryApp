using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.Common;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Application.Products;

/// <summary>
/// Coordinates product commands. Business rules remain in Domain; this class
/// coordinates repositories and commits the application operation.
/// </summary>
public sealed class ProductCommandService
{
    private readonly ICategoryReader _categoryReader;
    private readonly IProductReader _productReader;
    private readonly IProductWriter _productWriter;
    private readonly IProductVariantReader _variantReader;
    private readonly IProductVariantWriter _variantWriter;
    private readonly IUnitOfWork _unitOfWork;

    public ProductCommandService(
        ICategoryReader categoryReader,
        IProductReader productReader,
        IProductWriter productWriter,
        IProductVariantReader variantReader,
        IProductVariantWriter variantWriter,
        IUnitOfWork unitOfWork)
    {
        _categoryReader = categoryReader ?? throw new ArgumentNullException(nameof(categoryReader));
        _productReader = productReader ?? throw new ArgumentNullException(nameof(productReader));
        _productWriter = productWriter ?? throw new ArgumentNullException(nameof(productWriter));
        _variantReader = variantReader ?? throw new ArgumentNullException(nameof(variantReader));
        _variantWriter = variantWriter ?? throw new ArgumentNullException(nameof(variantWriter));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ProductDto> CreateProductAsync(
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

    public async Task<ProductVariantDto> CreateProductVariantAsync(
        CreateProductVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var productId = new ProductId(request.ProductId);
        await _productReader
            .GetByIdAsync(productId, cancellationToken)
            .RequireFoundAsync(productId.Value);

        var barcodes = request.Barcodes?
            .Select(barcode => barcode is null
                ? throw new ApplicationValidationException(
                    "Barcode collection cannot contain null items.",
                    nameof(request.Barcodes))
                : new ProductBarcode(barcode.Code, barcode.PackagingDescription))
            .ToArray();

        var variant = new ProductVariant(
            productId,
            request.Name,
            request.Sku,
            barcodes);

        await EnsureBarcodesAreUnassignedAsync(
            variant.Barcodes,
            currentVariantId: null,
            cancellationToken);

        await _variantWriter.AddAsync(variant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return variant.ToDto();
    }

    public async Task<ProductDto> UpdateProductAsync(
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

        var updatedProduct = product.Revise(
            categoryId,
            request.Name,
            new Money(request.PriceAmount, request.CurrencyCode),
            request.Description);

        await _productWriter.UpdateAsync(updatedProduct, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return updatedProduct.ToDto();
    }

    public async Task ArchiveProductAsync(
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

    public async Task<ProductVariantDto> AddBarcodeAsync(
        Guid variantId,
        AddProductVariantBarcodeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var typedVariantId = new ProductVariantId(variantId);
        var variant = await _variantReader
            .GetByIdAsync(typedVariantId, cancellationToken)
            .RequireFoundAsync(typedVariantId.Value);
        var barcode = new ProductBarcode(request.Code, request.PackagingDescription);

        await EnsureBarcodesAreUnassignedAsync(
            [barcode],
            variant.Id,
            cancellationToken);

        var updatedVariant = variant.AddBarcode(barcode);

        await _variantWriter.UpdateAsync(updatedVariant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return updatedVariant.ToDto();
    }

    public async Task<ProductVariantDto> RemoveBarcodeAsync(
        Guid variantId,
        RemoveProductVariantBarcodeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var typedVariantId = new ProductVariantId(variantId);
        var variant = await _variantReader
            .GetByIdAsync(typedVariantId, cancellationToken)
            .RequireFoundAsync(typedVariantId.Value);

        var updatedVariant = variant.RemoveBarcode(new ProductBarcode(request.Code));

        await _variantWriter.UpdateAsync(updatedVariant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return updatedVariant.ToDto();
    }

    public async Task<ProductVariantDto> ReviseBarcodeAsync(
        Guid variantId,
        ReviseProductVariantBarcodeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var typedVariantId = new ProductVariantId(variantId);
        var variant = await _variantReader
            .GetByIdAsync(typedVariantId, cancellationToken)
            .RequireFoundAsync(typedVariantId.Value);
        var revised = variant.ReviseBarcode(
            new ProductBarcode(request.Code, request.PackagingDescription));

        await _variantWriter.UpdateAsync(revised, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return revised.ToDto();
    }

    public async Task<ProductVariantDto> UpdateProductVariantAsync(
        Guid variantId,
        UpdateProductVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var typedVariantId = new ProductVariantId(variantId);
        var variant = await _variantReader
            .GetByIdAsync(typedVariantId, cancellationToken)
            .RequireFoundAsync(typedVariantId.Value);

        var updatedVariant = variant.Revise(request.Name, request.Sku);

        await _variantWriter.UpdateAsync(updatedVariant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return updatedVariant.ToDto();
    }

    public async Task ArchiveProductVariantAsync(
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        var typedVariantId = new ProductVariantId(variantId);
        await _variantReader
            .GetByIdAsync(typedVariantId, cancellationToken)
            .RequireFoundAsync(typedVariantId.Value);

        await _variantWriter.ArchiveAsync(typedVariantId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBarcodesAreUnassignedAsync(
        IEnumerable<ProductBarcode> barcodes,
        ProductVariantId? currentVariantId,
        CancellationToken cancellationToken)
    {
        var codes = barcodes
            .Select(barcode => barcode.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (codes.Length == 0)
        {
            return;
        }

        var reservedCodes = await _variantReader.FindReservedBarcodeCodesAsync(
            codes,
            currentVariantId,
            cancellationToken);

        if (reservedCodes.Count > 0)
        {
            throw new ProductBarcodeConflictException(
                reservedCodes.OrderBy(code => code, StringComparer.Ordinal).First());
        }
    }
}
