using WarrantyIssueXE.Domain.Enums;

namespace WarrantyIssueXE.Application.Warranties;

public sealed record CustomerInput(
    string FullName,
    string? Email = null,
    string? Phone = null,
    string? Address = null);

/// <summary>
/// Selects exactly one source for the coverage captured by an issued warranty.
/// </summary>
public abstract record CoverageInput;

public sealed record WarrantyPlanCoverageInput(Guid WarrantyPlanId) : CoverageInput;

public sealed record ExplicitCoverageInput(int TermMonths, string CoverageDescription)
    : CoverageInput;

public sealed record ReplacedPartInput(string Description, string? PartNumber = null);

public sealed record IssueWarrantyForSaleRequest(
    Guid ProductVariantId,
    CoverageInput Coverage,
    string? SerialNumber,
    CustomerInput Customer,
    DateOnly PurchasedOn,
    decimal PurchasePriceAmount,
    string PurchasePriceCurrencyCode);

public sealed record IssueWarrantyForRepairRequest(
    Guid ProductVariantId,
    CoverageInput Coverage,
    string? SerialNumber,
    CustomerInput Customer,
    DateOnly RepairedOn,
    string WorkDescription,
    IReadOnlyCollection<ReplacedPartInput>? ReplacedParts = null);

public sealed record CustomerSnapshotDto(
    string FullName,
    string? Email,
    string? Phone,
    string? Address);

public sealed record ProductSnapshotDto(string ProductName, string VariantName, string? Sku);

public sealed record IssuerSnapshotDto(
    string Name,
    string? VatNumber,
    string? Address,
    string? Email,
    string? Phone);

public sealed record CoverageSnapshotDto(
    int TermMonths,
    CoverageScope Scope,
    string CoverageDescription,
    string ScopeStatement);

public sealed record SaleDetailsDto(DateOnly PurchasedOn, decimal PurchasePriceAmount, string PurchasePriceCurrencyCode);

public sealed record ReplacedPartSnapshotDto(string Description, string? PartNumber);

public sealed record RepairDetailsDto(
    DateOnly RepairedOn,
    string WorkDescription,
    IReadOnlyList<ReplacedPartSnapshotDto> ReplacedParts);

public sealed record WarrantyDto(
    Guid Id,
    string CertificateNumber,
    WarrantyType Type,
    WarrantyStatus Status,
    Guid ProductVariantId,
    Guid? WarrantyPlanId,
    string? SerialNumber,
    DateTimeOffset IssuedAt,
    DateOnly StartsOn,
    DateOnly ExpiresOn,
    CustomerSnapshotDto Customer,
    ProductSnapshotDto Product,
    IssuerSnapshotDto Issuer,
    CoverageSnapshotDto Coverage,
    SaleDetailsDto? SaleDetails,
    RepairDetailsDto? RepairDetails,
    DateTimeOffset? VoidedAt,
    string? VoidReason);
