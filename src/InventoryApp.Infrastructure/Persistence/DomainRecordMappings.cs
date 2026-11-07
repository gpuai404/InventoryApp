using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Infrastructure.Persistence;

internal static class DomainRecordMappings
{
    public static Category ToDomain(this CategoryRecord record) =>
        new(record.Name, record.Description, new CategoryId(record.Id));

    public static CategoryRecord ToRecord(this Category category) =>
        new()
        {
            Id = category.Id.Value,
            Name = category.Name,
            Description = category.Description
        };

    public static Company ToDomain(this CompanyRecord record) =>
        new(
            record.Name,
            new CompanyContact(record.Address, record.Email, record.Phone),
            new CompanyId(record.Id));

    public static CompanyRecord ToRecord(this Company company) =>
        new()
        {
            Id = company.Id.Value,
            Name = company.Name,
            Address = company.Contact.Address,
            Email = company.Contact.Email,
            Phone = company.Contact.Phone
        };

    public static Product ToDomain(this ProductRecord record) =>
        new(
            new CategoryId(record.CategoryId),
            record.Name,
            new Money(record.PriceAmount, record.PriceCurrencyCode),
            record.Description,
            new ProductId(record.Id));

    public static ProductRecord ToRecord(this Product product) =>
        new()
        {
            Id = product.Id.Value,
            CategoryId = product.CategoryId.Value,
            Name = product.Name,
            PriceAmount = product.Price.Amount,
            PriceCurrencyCode = product.Price.CurrencyCode,
            Description = product.Description
        };

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

    public static Service ToDomain(this ServiceRecord record) =>
        new(
            new CompanyId(record.CompanyId),
            record.Name,
            new WarrantyTerm(record.WarrantyTermMonths),
            record.Description,
            new ServiceId(record.Id));

    public static ServiceRecord ToRecord(this Service service) =>
        new()
        {
            Id = service.Id.Value,
            CompanyId = service.CompanyId.Value,
            Name = service.Name,
            WarrantyTermMonths = service.WarrantyTerm.Months,
            Description = service.Description
        };

    public static Warranty ToDomain(this WarrantyRecord record) =>
        Warranty.Reconstitute(
            new ProductId(record.ProductId),
            new ServiceId(record.ServiceId),
            record.Description,
            record.StartsOn,
            record.ExpiresOn,
            new WarrantyId(record.Id));

    public static WarrantyRecord ToRecord(this Warranty warranty) =>
        new()
        {
            Id = warranty.Id.Value,
            ProductId = warranty.ProductId.Value,
            ServiceId = warranty.ServiceId.Value,
            Description = warranty.Description,
            StartsOn = warranty.StartsOn,
            ExpiresOn = warranty.ExpiresOn
        };

}
