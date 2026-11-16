namespace WarrantyIssueXE.Infrastructure.Persistence;

internal sealed class WarrantyRecord
{
    public Guid Id { get; set; }

    public string CertificateNumber { get; set; } = null!;

    public string Type { get; set; } = null!;

    public Guid? WarrantyPlanId { get; set; }

    public string? Imei { get; set; }

    public long IssuedAtUtcTicks { get; set; }

    public DateOnly StartsOn { get; set; }

    public DateOnly ExpiresOn { get; set; }

    public string CustomerFullName { get; set; } = null!;

    public string? CustomerEmail { get; set; }

    public string? CustomerPhone { get; set; }

    public string? CustomerAddress { get; set; }

    public string ProductName { get; set; } = null!;

    public Guid ProductId { get; set; }

    public Guid ProductCategoryId { get; set; }

    public string ProductCategoryName { get; set; } = null!;

    public decimal ProductPriceAmount { get; set; }

    public string ProductPriceCurrencyCode { get; set; } = null!;

    public string IssuerName { get; set; } = null!;

    public string? IssuerVatNumber { get; set; }

    public string IssuerAddress { get; set; } = null!;

    public string? IssuerEmail { get; set; }

    public string? IssuerPhone { get; set; }

    public int CoverageTermMonths { get; set; }

    public string CoverageScope { get; set; } = null!;

    public string CoverageDescription { get; set; } = null!;

    public string CoverageScopeStatement { get; set; } = null!;

    public DateOnly? PurchasedOn { get; set; }

    public DateOnly? RepairedOn { get; set; }

    public string? RepairWorkDescription { get; set; }

    public List<WarrantyReplacedPartRecord> ReplacedParts { get; set; } = [];
}
