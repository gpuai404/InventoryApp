using System.Collections.ObjectModel;
using InventoryApp.Domain.Exceptions;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.Primitives;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// A sellable or trackable variation of a product.
///
/// The product relationship is intentionally represented by <see cref="ProductId"/>
/// rather than an object reference, so persistence and application code can load
/// the related aggregate intentionally.
/// </summary>
public sealed class ProductVariant : Entity<ProductVariantId>
{
    private readonly ReadOnlyCollection<ProductBarcode> _barcodes;

    public ProductVariant(
        ProductId productId,
        string name,
        string? sku = null,
        IEnumerable<ProductBarcode>? barcodes = null,
        ProductVariantId? id = null)
        : base(id ?? ProductVariantId.New())
    {
        ProductId = DomainGuard.RequiredId(productId, nameof(productId));
        Name = DomainGuard.RequiredText(name, nameof(name), DomainTextLimits.Name);
        Sku = DomainGuard.OptionalText(
            sku,
            nameof(sku),
            DomainTextLimits.ProductVariantSku);

        _barcodes = CopyAndValidateBarcodes(barcodes);
    }

    public ProductId ProductId { get; private init; }

    public string Name { get; private init; }

    public string? Sku { get; private init; }

    public IReadOnlyList<ProductBarcode> Barcodes => _barcodes;

    /// <summary>
    /// Replaces the variant's editable details in one immutable transition.
    /// </summary>
    public ProductVariant Revise(
        string name,
        string? sku,
        IEnumerable<ProductBarcode> barcodes) =>
        new(
            ProductId,
            name,
            sku,
            DomainGuard.Required(barcodes, nameof(barcodes)),
            Id);

    /// <summary>
    /// Creates a variant with one additional barcode.
    /// </summary>
    public ProductVariant AddBarcode(ProductBarcode barcode)
    {
        var validatedBarcode = DomainGuard.Required(barcode, nameof(barcode));

        if (ContainsBarcode(validatedBarcode))
        {
            throw ProductVariantExceptions.BarcodeAlreadyAssigned(
                validatedBarcode.Code,
                nameof(barcode));
        }

        return Revise(Name, Sku, _barcodes.Append(validatedBarcode));
    }

    /// <summary>
    /// Creates a variant without the supplied barcode.
    /// </summary>
    public ProductVariant RemoveBarcode(ProductBarcode barcode)
    {
        var validatedBarcode = DomainGuard.Required(barcode, nameof(barcode));

        if (!ContainsBarcode(validatedBarcode))
        {
            throw ProductVariantExceptions.BarcodeNotAssigned(
                validatedBarcode.Code,
                nameof(barcode));
        }

        return Revise(
            Name,
            Sku,
            _barcodes.Where(candidate =>
                !string.Equals(candidate.Code, validatedBarcode.Code, StringComparison.Ordinal)));
    }

    private static ReadOnlyCollection<ProductBarcode> CopyAndValidateBarcodes(
        IEnumerable<ProductBarcode>? barcodes)
    {
        var barcodeList = barcodes?.ToArray() ?? [];
        if (barcodeList.Any(barcode => barcode is null))
        {
            throw ProductVariantExceptions.BarcodeCollectionContainsNull(nameof(barcodes));
        }

        if (barcodeList
            .Select(barcode => barcode.Code)
            .Distinct(StringComparer.Ordinal)
            .Count() != barcodeList.Length)
        {
            throw ProductVariantExceptions.BarcodeCollectionContainsDuplicates(nameof(barcodes));
        }

        return Array.AsReadOnly(barcodeList);
    }

    private bool ContainsBarcode(ProductBarcode barcode) =>
        _barcodes.Any(candidate =>
            string.Equals(candidate.Code, barcode.Code, StringComparison.Ordinal));
}
