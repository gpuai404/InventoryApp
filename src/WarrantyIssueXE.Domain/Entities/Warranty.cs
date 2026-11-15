using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.Primitives;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Domain.WarrantyDetails;

namespace WarrantyIssueXE.Domain.Entities;

/// <summary>
/// An immutable issued warranty certificate. The aggregate has exactly two
/// valid shapes: product-sale coverage with sale details, or repair coverage
/// with repair details.
/// </summary>
public sealed class Warranty : Entity<WarrantyId>
{
    private Warranty(
        string certificateNumber,
        WarrantyType type,
        WarrantyPlanId? warrantyPlanId,
        Imei? imei,
        DateTimeOffset issuedAt,
        CustomerSnapshot customer,
        ProductSnapshot product,
        IssuerSnapshot issuer,
        CoverageSnapshot coverage,
        SaleDetails? saleDetails,
        RepairDetails? repairDetails,
        DateOnly startsOn,
        DateOnly expiresOn,
        WarrantyId? id)
        : base(id ?? WarrantyId.New())
    {
        CertificateNumber = NormalizeCertificateNumber(certificateNumber);
        Type = DomainGuard.DefinedEnum(type, nameof(type));
        if (warrantyPlanId is { } selectedWarrantyPlanId)
        {
            DomainGuard.RequiredId(selectedWarrantyPlanId, nameof(warrantyPlanId));
        }

        WarrantyPlanId = warrantyPlanId;
        Imei = imei;
        IssuedAt = NormalizeUtc(issuedAt, nameof(issuedAt));
        Customer = DomainGuard.Required(customer, nameof(customer));
        Product = DomainGuard.Required(product, nameof(product));
        Issuer = DomainGuard.Required(issuer, nameof(issuer));
        Coverage = DomainGuard.Required(coverage, nameof(coverage));
        SaleDetails = saleDetails;
        RepairDetails = repairDetails;
        StartsOn = DomainGuard.RequiredDate(startsOn, nameof(startsOn));
        ExpiresOn = DomainGuard.RequiredDate(expiresOn, nameof(expiresOn));

        ValidateTypeSpecificState();

        if (ExpiresOn < StartsOn)
        {
            throw WarrantyExceptions.ExpirationPrecedesStart(nameof(expiresOn));
        }

        if (ExpiresOn != Coverage.Term.ExpiresOn(StartsOn))
        {
            throw WarrantyExceptions.ExpirationDoesNotMatchTerm(nameof(expiresOn));
        }
    }

    public string CertificateNumber { get; }

    public WarrantyType Type { get; }

    /// <summary>
    /// The optional reusable policy from which this certificate originated.
    /// Coverage remains an independent historical snapshot.
    /// </summary>
    public WarrantyPlanId? WarrantyPlanId { get; }

    public Imei? Imei { get; }

    /// <summary>
    /// UTC instant at which the certificate was issued.
    /// </summary>
    public DateTimeOffset IssuedAt { get; }

    public DateOnly StartsOn { get; }

    public DateOnly ExpiresOn { get; }

    public CustomerSnapshot Customer { get; }

    public ProductSnapshot Product { get; }

    public IssuerSnapshot Issuer { get; }

    public CoverageSnapshot Coverage { get; }

    public SaleDetails? SaleDetails { get; }

    public RepairDetails? RepairDetails { get; }

    /// <summary>
    /// Produces the canonical storage and comparison form for certificate
    /// numbers. This keeps a regular unique index portable across providers.
    /// </summary>
    public static string NormalizeCertificateNumber(string certificateNumber)
    {
        var trimmedCertificateNumber = DomainGuard.RequiredText(
            certificateNumber,
            nameof(certificateNumber),
            maximumLength: null);

        return DomainGuard.RequiredText(
            trimmedCertificateNumber.ToUpperInvariant(),
            nameof(certificateNumber),
            DomainTextLimits.WarrantyCertificateNumber);
    }

    public static Warranty IssueForSale(
        string certificateNumber,
        WarrantyPlanId? warrantyPlanId,
        Imei? imei,
        DateTimeOffset issuedAt,
        CustomerSnapshot customer,
        ProductSnapshot product,
        IssuerSnapshot issuer,
        CoverageSnapshot coverage,
        SaleDetails saleDetails,
        WarrantyId? id = null)
    {
        var validatedSaleDetails = DomainGuard.Required(saleDetails, nameof(saleDetails));

        return IssueForSale(
            certificateNumber,
            warrantyPlanId,
            imei,
            issuedAt,
            validatedSaleDetails.PurchasedOn,
            customer,
            product,
            issuer,
            coverage,
            validatedSaleDetails,
            id);
    }

    public static Warranty IssueForSale(
        string certificateNumber,
        WarrantyPlanId? warrantyPlanId,
        Imei? imei,
        DateTimeOffset issuedAt,
        DateOnly issuanceBusinessDate,
        CustomerSnapshot customer,
        ProductSnapshot product,
        IssuerSnapshot issuer,
        CoverageSnapshot coverage,
        SaleDetails saleDetails,
        WarrantyId? id = null)
    {
        var validatedSaleDetails = DomainGuard.Required(saleDetails, nameof(saleDetails));
        var validatedCoverage = DomainGuard.Required(coverage, nameof(coverage));
        EnsureNotAfterIssuanceBusinessDate(
            validatedSaleDetails.PurchasedOn,
            issuanceBusinessDate,
            nameof(saleDetails));

        return new Warranty(
            certificateNumber,
            WarrantyType.ProductSale,
            warrantyPlanId,
            imei,
            issuedAt,
            customer,
            product,
            issuer,
            validatedCoverage,
            validatedSaleDetails,
            repairDetails: null,
            startsOn: validatedSaleDetails.PurchasedOn,
            expiresOn: validatedCoverage.Term.ExpiresOn(validatedSaleDetails.PurchasedOn),
            id: id);
    }

    public static Warranty IssueForRepair(
        string certificateNumber,
        WarrantyPlanId? warrantyPlanId,
        Imei? imei,
        DateTimeOffset issuedAt,
        CustomerSnapshot customer,
        ProductSnapshot product,
        IssuerSnapshot issuer,
        CoverageSnapshot coverage,
        RepairDetails repairDetails,
        WarrantyId? id = null)
    {
        var validatedRepairDetails = DomainGuard.Required(repairDetails, nameof(repairDetails));

        return IssueForRepair(
            certificateNumber,
            warrantyPlanId,
            imei,
            issuedAt,
            validatedRepairDetails.RepairedOn,
            customer,
            product,
            issuer,
            coverage,
            validatedRepairDetails,
            id);
    }

    public static Warranty IssueForRepair(
        string certificateNumber,
        WarrantyPlanId? warrantyPlanId,
        Imei? imei,
        DateTimeOffset issuedAt,
        DateOnly issuanceBusinessDate,
        CustomerSnapshot customer,
        ProductSnapshot product,
        IssuerSnapshot issuer,
        CoverageSnapshot coverage,
        RepairDetails repairDetails,
        WarrantyId? id = null)
    {
        var validatedRepairDetails = DomainGuard.Required(repairDetails, nameof(repairDetails));
        var validatedCoverage = DomainGuard.Required(coverage, nameof(coverage));
        EnsureNotAfterIssuanceBusinessDate(
            validatedRepairDetails.RepairedOn,
            issuanceBusinessDate,
            nameof(repairDetails));

        return new Warranty(
            certificateNumber,
            WarrantyType.Repair,
            warrantyPlanId,
            imei,
            issuedAt,
            customer,
            product,
            issuer,
            validatedCoverage,
            saleDetails: null,
            repairDetails: validatedRepairDetails,
            startsOn: validatedRepairDetails.RepairedOn,
            expiresOn: validatedCoverage.Term.ExpiresOn(validatedRepairDetails.RepairedOn),
            id: id);
    }

    /// <summary>
    /// Reconstitutes a warranty already persisted by the application. This is a
    /// persistence boundary, not an alternative issuance path: it validates all
    /// invariants while retaining the original snapshots and identifier.
    /// </summary>
    public static Warranty Reconstitute(
        string certificateNumber,
        WarrantyType type,
        WarrantyPlanId? warrantyPlanId,
        Imei? imei,
        DateTimeOffset issuedAt,
        CustomerSnapshot customer,
        ProductSnapshot product,
        IssuerSnapshot issuer,
        CoverageSnapshot coverage,
        SaleDetails? saleDetails,
        RepairDetails? repairDetails,
        DateOnly startsOn,
        DateOnly expiresOn,
        WarrantyId id)
    {
        var validatedType = DomainGuard.DefinedEnum(type, nameof(type));

        return new Warranty(
            certificateNumber,
            validatedType,
            warrantyPlanId,
            imei,
            issuedAt,
            customer,
            product,
            issuer,
            coverage,
            saleDetails,
            repairDetails,
            startsOn,
            expiresOn,
            id);
    }

    public bool IsActiveOn(DateOnly date) => date >= StartsOn && date <= ExpiresOn;

    private void ValidateTypeSpecificState()
    {
        if (Type == WarrantyType.ProductSale)
        {
            if (SaleDetails is null)
            {
                throw WarrantyExceptions.DetailsInvalid(
                    nameof(SaleDetails),
                    "A product-sale warranty requires sale details.");
            }

            if (RepairDetails is not null)
            {
                throw WarrantyExceptions.DetailsInvalid(
                    nameof(RepairDetails),
                    "A product-sale warranty cannot contain repair details.");
            }

            if (Coverage.Scope != CoverageScope.Product)
            {
                throw WarrantyExceptions.CoverageInvalid(
                    nameof(Coverage.Scope),
                    "A product-sale warranty must use product coverage scope.");
            }

            if (StartsOn != SaleDetails.PurchasedOn)
            {
                throw WarrantyExceptions.DetailsInvalid(
                    nameof(StartsOn),
                    "A product-sale warranty must start on the purchase date.");
            }

            return;
        }

        if (RepairDetails is null)
        {
            throw WarrantyExceptions.DetailsInvalid(
                nameof(RepairDetails),
                "A repair warranty requires repair details.");
        }

        if (SaleDetails is not null)
        {
            throw WarrantyExceptions.DetailsInvalid(
                nameof(SaleDetails),
                "A repair warranty cannot contain sale details.");
        }

        if (Coverage.Scope != CoverageScope.RepairWorkAndReplacedParts)
        {
            throw WarrantyExceptions.CoverageInvalid(
                nameof(Coverage.Scope),
                "A repair warranty must use repair-work-and-replaced-parts coverage scope.");
        }

        if (StartsOn != RepairDetails.RepairedOn)
        {
            throw WarrantyExceptions.DetailsInvalid(
                nameof(StartsOn),
                "A repair warranty must start on the repaired date.");
        }
    }

    private static DateTimeOffset NormalizeUtc(DateTimeOffset value, string parameterName)
    {
        if (value == default)
        {
            throw WarrantyExceptions.TimestampRequired(parameterName);
        }

        return value.ToUniversalTime();
    }

    private static void EnsureNotAfterIssuanceBusinessDate(
        DateOnly businessDate,
        DateOnly issuanceBusinessDate,
        string propertyName)
    {
        DomainGuard.RequiredDate(issuanceBusinessDate, nameof(issuanceBusinessDate));
        if (businessDate > issuanceBusinessDate)
        {
            throw WarrantyExceptions.BusinessDateIsInFuture(propertyName);
        }
    }
}
