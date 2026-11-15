using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Infrastructure.Persistence;

namespace WarrantyIssueXE.Infrastructure.Persistence.Mappings;

internal static partial class DomainRecordMappings
{
    public static ProductVariant ToDomain(this ProductVariantRecord record) =>
        new(
            new ProductId(record.ProductId),
            record.Name,
            record.Sku,
            record.Barcodes
                .OrderBy(barcode => barcode.Code, StringComparer.Ordinal)
                .Select(barcode => barcode.ToDomain()),
            new ProductVariantId(record.Id));

    public static ProductVariantRecord ToRecord(this ProductVariant variant) =>
        new()
        {
            Id = variant.Id.Value,
            ProductId = variant.ProductId.Value,
            Name = variant.Name,
            Sku = variant.Sku,
            Barcodes = variant.Barcodes
                .Select(barcode => barcode.ToRecord(variant.Id))
                .ToList()
        };

    public static ProductBarcode ToDomain(this ProductBarcodeRecord record) =>
        new(record.Code, record.PackagingDescription);

    public static ProductBarcodeRecord ToRecord(
        this ProductBarcode barcode,
        ProductVariantId productVariantId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProductVariantId = productVariantId.Value,
            Code = barcode.Code,
            PackagingDescription = barcode.PackagingDescription
        };
}
