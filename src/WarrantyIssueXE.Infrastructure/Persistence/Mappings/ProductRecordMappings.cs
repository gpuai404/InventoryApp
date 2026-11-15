using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Infrastructure.Persistence;

namespace WarrantyIssueXE.Infrastructure.Persistence.Mappings;

internal static partial class DomainRecordMappings
{
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
}
