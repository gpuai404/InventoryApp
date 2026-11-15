using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Domain.WarrantyDetails;
using WarrantyIssueXE.Infrastructure.Persistence;

namespace WarrantyIssueXE.Infrastructure.Persistence.Mappings;

internal static partial class DomainRecordMappings
{
    public static Warranty ToDomain(this WarrantyRecord record)
    {
        var coverage = CoverageSnapshot.Reconstitute(
            new WarrantyTerm(record.CoverageTermMonths),
            ParseEnum<CoverageScope>(record.CoverageScope, nameof(WarrantyRecord.CoverageScope)),
            record.CoverageDescription,
            record.CoverageScopeStatement);

        SaleDetails? saleDetails = record.PurchasedOn is { } purchasedOn
            ? new SaleDetails(
                purchasedOn,
                new Money(
                    record.PurchasePriceAmount ?? throw MissingRequiredValue(nameof(record.PurchasePriceAmount)),
                    record.PurchasePriceCurrencyCode ?? throw MissingRequiredValue(nameof(record.PurchasePriceCurrencyCode))))
            : null;

        RepairDetails? repairDetails = record.RepairedOn is { } repairedOn
            ? new RepairDetails(
                repairedOn,
                record.RepairWorkDescription ?? throw MissingRequiredValue(nameof(record.RepairWorkDescription)),
                record.ReplacedParts
                    .OrderBy(part => part.Position)
                    .Select(part => new ReplacedPartSnapshot(part.Description, part.PartNumber)))
            : null;

        var type = ParseEnum<WarrantyType>(record.Type, nameof(WarrantyRecord.Type));
        var status = ParseEnum<WarrantyStatus>(record.Status, nameof(WarrantyRecord.Status));
        var productVariantId = new ProductVariantId(record.ProductVariantId);
        WarrantyPlanId? warrantyPlanId = record.WarrantyPlanId is { } planId
            ? new WarrantyPlanId(planId)
            : null;
        var issuedAt = FromUtcTicks(record.IssuedAtUtcTicks, nameof(record.IssuedAtUtcTicks));
        var customer = new CustomerSnapshot(
            record.CustomerFullName,
            record.CustomerEmail,
            record.CustomerPhone,
            record.CustomerAddress);
        var product = new ProductSnapshot(record.ProductName, record.ProductVariantName, record.ProductSku);
        var issuer = new IssuerSnapshot(
            record.IssuerName,
            record.IssuerVatNumber,
            record.IssuerAddress,
            record.IssuerEmail,
            record.IssuerPhone);
        DateTimeOffset? voidedAt = record.VoidedAtUtcTicks is { } ticks
            ? FromUtcTicks(ticks, nameof(record.VoidedAtUtcTicks))
            : null;
        var warrantyId = new WarrantyId(record.Id);

        if (type == WarrantyType.ProductSale &&
            (repairDetails is not null ||
             record.RepairWorkDescription is not null ||
             record.ReplacedParts.Count > 0))
        {
            throw new InvalidOperationException(
                "A persisted product-sale warranty cannot contain repair details.");
        }

        if (type == WarrantyType.Repair &&
            (saleDetails is not null ||
             record.PurchasePriceAmount is not null ||
             record.PurchasePriceCurrencyCode is not null))
        {
            throw new InvalidOperationException(
                "A persisted repair warranty cannot contain sale details.");
        }

        return type == WarrantyType.ProductSale
            ? Warranty.ReconstituteSale(
                record.CertificateNumber,
                status,
                productVariantId,
                warrantyPlanId,
                record.SerialNumber,
                issuedAt,
                customer,
                product,
                issuer,
                coverage,
                saleDetails ?? throw MissingRequiredValue(nameof(record.PurchasedOn)),
                record.StartsOn,
                record.ExpiresOn,
                voidedAt,
                record.VoidReason,
                warrantyId)
            : Warranty.ReconstituteRepair(
                record.CertificateNumber,
                status,
                productVariantId,
                warrantyPlanId,
                record.SerialNumber,
                issuedAt,
                customer,
                product,
                issuer,
                coverage,
                repairDetails ?? throw MissingRequiredValue(nameof(record.RepairedOn)),
                record.StartsOn,
                record.ExpiresOn,
                voidedAt,
                record.VoidReason,
                warrantyId);
    }

    public static WarrantyRecord ToRecord(this Warranty warranty) =>
        new()
        {
            Id = warranty.Id.Value,
            CertificateNumber = Warranty.NormalizeCertificateNumber(warranty.CertificateNumber),
            Type = warranty.Type.ToString(),
            Status = warranty.Status.ToString(),
            ProductVariantId = warranty.ProductVariantId.Value,
            WarrantyPlanId = warranty.WarrantyPlanId?.Value,
            SerialNumber = warranty.SerialNumber,
            IssuedAtUtcTicks = ToUtcTicks(warranty.IssuedAt),
            StartsOn = warranty.StartsOn,
            ExpiresOn = warranty.ExpiresOn,
            VoidedAtUtcTicks = warranty.VoidedAt is { } voidedAt ? ToUtcTicks(voidedAt) : null,
            VoidReason = warranty.VoidReason,
            CustomerFullName = warranty.Customer.FullName,
            CustomerEmail = warranty.Customer.Email,
            CustomerPhone = warranty.Customer.Phone,
            CustomerAddress = warranty.Customer.Address,
            ProductName = warranty.Product.ProductName,
            ProductVariantName = warranty.Product.VariantName,
            ProductSku = warranty.Product.Sku,
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
            PurchasePriceAmount = warranty.SaleDetails?.PurchasePrice.Amount,
            PurchasePriceCurrencyCode = warranty.SaleDetails?.PurchasePrice.CurrencyCode,
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
