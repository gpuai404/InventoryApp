using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Domain.WarrantyDetails;

namespace WarrantyIssueXE.Domain.Tests;

public sealed class WarrantyTests
{
    private static readonly DateTimeOffset IssuedAt =
        new(2026, 1, 2, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Sale_issuance_captures_an_immutable_historical_certificate()
    {
        var product = ProductSnapshot();
        var issuer = IssuerSnapshot();
        var customer = new CustomerSnapshot("Ada", "ada@example.test");
        var imei = new Imei("490154203237518");
        var coverage = CoverageSnapshot.ForProductSale(new WarrantyTerm(24), "Parts and labour");

        var warranty = Warranty.IssueForSale(
            "wr-sale-001",
            WarrantyPlanId.New(),
            imei,
            IssuedAt,
            new DateOnly(2026, 1, 2),
            customer,
            product,
            issuer,
            coverage,
            new SaleDetails(new DateOnly(2026, 1, 1)));

        Assert.Equal("WR-SALE-001", warranty.CertificateNumber);
        Assert.Equal(WarrantyType.ProductSale, warranty.Type);
        Assert.Equal(imei, warranty.Imei);
        Assert.Equal(customer, warranty.Customer);
        Assert.Equal(product, warranty.Product);
        Assert.Equal(issuer, warranty.Issuer);
        Assert.Equal(new DateOnly(2026, 1, 1), warranty.StartsOn);
        Assert.Equal(new DateOnly(2027, 12, 31), warranty.ExpiresOn);
        Assert.NotNull(warranty.SaleDetails);
        Assert.Null(warranty.RepairDetails);
    }

    [Fact]
    public void Repair_issuance_supports_labor_only_work_and_optional_imei()
    {
        var details = new RepairDetails(new DateOnly(2026, 1, 1), "Repaired charging connector");

        var warranty = Warranty.IssueForRepair(
            "wr-repair-001",
            WarrantyPlanId.New(),
            null,
            IssuedAt,
            new DateOnly(2026, 1, 2),
            new CustomerSnapshot("Ada"),
            ProductSnapshot(),
            IssuerSnapshot(),
            CoverageSnapshot.ForRepair(new WarrantyTerm(6), "Repair work and replaced parts"),
            details);

        Assert.Equal(WarrantyType.Repair, warranty.Type);
        Assert.Null(warranty.Imei);
        Assert.Empty(warranty.RepairDetails!.ReplacedParts);
        Assert.Null(warranty.SaleDetails);
    }

    [Fact]
    public void Product_sale_rejects_repair_coverage()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() => Warranty.IssueForSale(
            "WR-SALE-001",
            WarrantyPlanId.New(),
            null,
            IssuedAt,
            new DateOnly(2026, 1, 2),
            new CustomerSnapshot("Ada"),
            ProductSnapshot(),
            IssuerSnapshot(),
            CoverageSnapshot.ForRepair(new WarrantyTerm(6), "Repair work"),
            new SaleDetails(new DateOnly(2026, 1, 1))));

        Assert.Equal(DomainErrorCodes.WarrantyCoverageInvalid, exception.Code);
    }

    [Fact]
    public void Repair_rejects_product_sale_coverage()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() => Warranty.IssueForRepair(
            "WR-REPAIR-001",
            WarrantyPlanId.New(),
            null,
            IssuedAt,
            new DateOnly(2026, 1, 2),
            new CustomerSnapshot("Ada"),
            ProductSnapshot(),
            IssuerSnapshot(),
            CoverageSnapshot.ForProductSale(new WarrantyTerm(24), "Product coverage"),
            new RepairDetails(new DateOnly(2026, 1, 1), "Repaired battery")));

        Assert.Equal(DomainErrorCodes.WarrantyCoverageInvalid, exception.Code);
    }

    [Fact]
    public void Repair_issuance_requires_an_issuer_snapshot()
    {
        Assert.Throws<DomainValidationException>(() => Warranty.IssueForRepair(
            "WR-REPAIR-001",
            WarrantyPlanId.New(),
            null,
            IssuedAt,
            new DateOnly(2026, 1, 2),
            new CustomerSnapshot("Ada"),
            ProductSnapshot(),
            null!,
            CoverageSnapshot.ForRepair(new WarrantyTerm(6), "Repair work"),
            new RepairDetails(new DateOnly(2026, 1, 1), "Repaired battery")));
    }

    [Fact]
    public void Reconstitution_rejects_an_expiry_not_derived_from_the_term()
    {
        var details = new SaleDetails(new DateOnly(2026, 1, 1));

        var exception = Assert.Throws<DomainRuleViolationException>(() => Warranty.Reconstitute(
            "WR-HISTORY-001",
            WarrantyType.ProductSale,
            null,
            null,
            IssuedAt,
            new CustomerSnapshot("Ada"),
            ProductSnapshot(),
            IssuerSnapshot(),
            CoverageSnapshot.ForProductSale(new WarrantyTerm(12), "Product coverage"),
            details,
            null,
            details.PurchasedOn,
            new DateOnly(2027, 1, 1),
            WarrantyId.New()));

        Assert.Equal(DomainErrorCodes.WarrantyPeriodInvalid, exception.Code);
    }

    [Fact]
    public void Issued_warranty_properties_are_read_only()
    {
        Assert.All(
            typeof(Warranty).GetProperties(),
            property => Assert.False(property.SetMethod?.IsPublic ?? false));
    }

    [Fact]
    public void Sale_issuance_rejects_a_purchase_date_after_the_issuance_business_date()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() => Warranty.IssueForSale(
            "WR-FUTURE-SALE",
            WarrantyPlanId.New(),
            null,
            IssuedAt,
            new DateOnly(2026, 1, 2),
            new CustomerSnapshot("Ada"),
            ProductSnapshot(),
            IssuerSnapshot(),
            CoverageSnapshot.ForProductSale(new WarrantyTerm(12), "Product coverage"),
            new SaleDetails(new DateOnly(2026, 1, 3))));

        Assert.Equal(DomainErrorCodes.WarrantyBusinessDateInvalid, exception.Code);
    }

    [Fact]
    public void Repair_issuance_rejects_a_repair_date_after_the_issuance_business_date()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() => Warranty.IssueForRepair(
            "WR-FUTURE-REPAIR",
            WarrantyPlanId.New(),
            null,
            IssuedAt,
            new DateOnly(2026, 1, 2),
            new CustomerSnapshot("Ada"),
            ProductSnapshot(),
            IssuerSnapshot(),
            CoverageSnapshot.ForRepair(new WarrantyTerm(6), "Repair work"),
            new RepairDetails(new DateOnly(2026, 1, 3), "Repaired battery")));

        Assert.Equal(DomainErrorCodes.WarrantyBusinessDateInvalid, exception.Code);
    }

    [Fact]
    public void Reconstitution_preserves_historical_repair_scope_wording()
    {
        const string historicalWording =
            "This warranty covers the completed repair under the terms used when it was issued.";

        var coverage = CoverageSnapshot.Reconstitute(
            new WarrantyTerm(6),
            CoverageScope.RepairWorkAndReplacedParts,
            "Repair work",
            historicalWording);

        Assert.Equal(historicalWording, coverage.ScopeStatement);
    }

    private static ProductSnapshot ProductSnapshot() =>
        new(
            ProductId.New(),
            "Laptop",
            CategoryId.New(),
            "Computers",
            new Money(899.99m, "EUR"));

    private static IssuerSnapshot IssuerSnapshot() =>
        new("Contoso Retail", "BE 0123.456.789", "Avenue des Arts 1");
}
