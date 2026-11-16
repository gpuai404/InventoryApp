using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Domain.WarrantyDetails;

namespace WarrantyIssueXE.Infrastructure.Persistence.Mappings;

internal static partial class DomainRecordMappings
{
    public static Warranty ToDomain(this WarrantyRecord record)
    {
        var type = ParseEnum<WarrantyType>(record.Type, nameof(WarrantyRecord.Type));
        ValidatePersistedShape(record, type);

        var coverage = CoverageSnapshot.Reconstitute(
            new WarrantyTerm(record.CoverageTermMonths),
            ParseEnum<CoverageScope>(record.CoverageScope, nameof(WarrantyRecord.CoverageScope)),
            record.CoverageDescription,
            record.CoverageScopeStatement);
        var saleDetails = type == WarrantyType.ProductSale
            ? new SaleDetails(record.PurchasedOn ?? throw MissingRequiredValue(nameof(record.PurchasedOn)))
            : null;
        var repairDetails = type == WarrantyType.Repair
            ? new RepairDetails(
                record.RepairedOn ?? throw MissingRequiredValue(nameof(record.RepairedOn)),
                record.RepairWorkDescription ?? throw MissingRequiredValue(nameof(record.RepairWorkDescription)),
                record.ReplacedParts
                    .OrderBy(part => part.Position)
                    .Select(part => new ReplacedPartSnapshot(part.Description, part.PartNumber)))
            : null;
        WarrantyPlanId? warrantyPlanId = record.WarrantyPlanId is { } planId
            ? new WarrantyPlanId(planId)
            : null;
        Imei? imei = record.Imei is { } value ? new Imei(value) : null;

        return Warranty.Reconstitute(
            record.CertificateNumber,
            type,
            warrantyPlanId,
            imei,
            FromUtcTicks(record.IssuedAtUtcTicks, nameof(record.IssuedAtUtcTicks)),
            new CustomerSnapshot(
                record.CustomerFullName,
                record.CustomerEmail,
                record.CustomerPhone,
                record.CustomerAddress),
            new ProductSnapshot(
                new ProductId(record.ProductId),
                record.ProductName,
                new CategoryId(record.ProductCategoryId),
                record.ProductCategoryName,
                new Money(record.ProductPriceAmount, record.ProductPriceCurrencyCode)),
            new IssuerSnapshot(
                record.IssuerName,
                record.IssuerVatNumber,
                record.IssuerAddress,
                record.IssuerEmail,
                record.IssuerPhone),
            coverage,
            saleDetails,
            repairDetails,
            record.StartsOn,
            record.ExpiresOn,
            new WarrantyId(record.Id));
    }

    public static WarrantyRecord ToRecord(this Warranty warranty) =>
        new()
        {
            Id = warranty.Id.Value,
            CertificateNumber = Warranty.NormalizeCertificateNumber(warranty.CertificateNumber),
            Type = warranty.Type.ToString(),
            WarrantyPlanId = warranty.WarrantyPlanId?.Value,
            Imei = warranty.Imei?.Value,
            IssuedAtUtcTicks = ToUtcTicks(warranty.IssuedAt),
            StartsOn = warranty.StartsOn,
            ExpiresOn = warranty.ExpiresOn,
            CustomerFullName = warranty.Customer.FullName,
            CustomerEmail = warranty.Customer.Email,
            CustomerPhone = warranty.Customer.Phone,
            CustomerAddress = warranty.Customer.Address,
            ProductId = warranty.Product.ProductId.Value,
            ProductName = warranty.Product.ProductName,
            ProductCategoryId = warranty.Product.CategoryId.Value,
            ProductCategoryName = warranty.Product.CategoryName,
            ProductPriceAmount = warranty.Product.Price.Amount,
            ProductPriceCurrencyCode = warranty.Product.Price.CurrencyCode,
            IssuerName = warranty.Issuer.Name,
            IssuerVatNumber = warranty.Issuer.VatNumber,
            IssuerAddress = warranty.Issuer.Address,
            IssuerEmail = warranty.Issuer.Email,
            IssuerPhone = warranty.Issuer.Phone,
            CoverageTermMonths = warranty.Coverage.Term.Months,
            CoverageScope = warranty.Coverage.Scope.ToString(),
            CoverageDescription = warranty.Coverage.CoverageDescription,
            CoverageScopeStatement = warranty.Coverage.ScopeStatement,
            PurchasedOn = warranty.SaleDetails?.PurchasedOn,
            RepairedOn = warranty.RepairDetails?.RepairedOn,
            RepairWorkDescription = warranty.RepairDetails?.WorkDescription,
            ReplacedParts = warranty.RepairDetails?.ReplacedParts
                .Select((part, index) => new WarrantyReplacedPartRecord
                {
                    WarrantyId = warranty.Id.Value,
                    Position = index,
                    Description = part.Description,
                    PartNumber = part.PartNumber
                })
                .ToList() ?? []
        };

    public static long ToUtcTicks(DateTimeOffset value) => value.ToUniversalTime().Ticks;

    public static DateTimeOffset FromUtcTicks(long utcTicks, string propertyName)
    {
        try
        {
            return new DateTimeOffset(utcTicks, TimeSpan.Zero);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidOperationException(
                $"Persisted UTC ticks in '{propertyName}' are invalid.",
                exception);
        }
    }

    private static void ValidatePersistedShape(WarrantyRecord record, WarrantyType type)
    {
        if (type == WarrantyType.ProductSale &&
            (record.RepairedOn is not null ||
             record.RepairWorkDescription is not null ||
             record.ReplacedParts.Count > 0))
        {
            throw new InvalidOperationException("A persisted product-sale warranty cannot contain repair details.");
        }

        if (type == WarrantyType.Repair && record.PurchasedOn is not null)
        {
            throw new InvalidOperationException("A persisted repair warranty cannot contain sale details.");
        }
    }

    private static TEnum ParseEnum<TEnum>(string value, string propertyName)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, ignoreCase: false, out var result) && Enum.IsDefined(result))
        {
            return result;
        }

        throw new InvalidOperationException(
            $"Persisted enum value '{value}' for '{propertyName}' is invalid.");
    }

    private static InvalidOperationException MissingRequiredValue(string propertyName) =>
        new($"Persisted warranty field '{propertyName}' is required by its warranty type.");
}
