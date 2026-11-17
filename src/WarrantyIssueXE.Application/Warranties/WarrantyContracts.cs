using WarrantyIssueXE.Domain.Enums;

namespace WarrantyIssueXE.Application.Warranties;

public sealed record CustomerInput(
    string FullName,
    string? Email = null,
    string? Phone = null,
    string? Address = null);

public sealed record ReplacedPartInput(string Description, string? PartNumber = null);

public sealed record IssueProductSaleWarrantyRequest(
    Guid ProductId,
    Guid WarrantyPlanId,
    string? Imei,
    CustomerInput Customer,
    DateOnly PurchasedOn);

public sealed record IssueRepairWarrantyRequest(
    Guid ProductId,
    Guid WarrantyPlanId,
    string? Imei,
    CustomerInput Customer,
    DateOnly RepairedOn,
    string WorkDescription,
    IReadOnlyCollection<ReplacedPartInput>? ReplacedParts = null);

public sealed record CustomerSnapshotDto(
    string FullName,
    string? Email,
    string? Phone,
    string? Address);

public sealed record ProductSnapshotDto(
    Guid ProductId,
    string ProductName,
    Guid CategoryId,
    string CategoryName,
    decimal PriceAmount,
    string PriceCurrencyCode);

public sealed record IssuerSnapshotDto(
    string Name,
    string? VatNumber,
    string Address,
    string? Email,
    string? Phone);

public sealed record CoverageSnapshotDto(
    int TermMonths,
    CoverageScope Scope,
    string CoverageDescription,
    string ScopeStatement);

public sealed record SaleDetailsDto(DateOnly PurchasedOn);

public sealed record ReplacedPartSnapshotDto(string Description, string? PartNumber);

public sealed record RepairDetailsDto(
    DateOnly RepairedOn,
    string WorkDescription,
    IReadOnlyList<ReplacedPartSnapshotDto> ReplacedParts);

public sealed record WarrantyDto(
    Guid Id,
    string CertificateNumber,
    WarrantyType Type,
    Guid? WarrantyPlanId,
    string? Imei,
    DateTimeOffset IssuedAt,
    DateOnly StartsOn,
    DateOnly ExpiresOn,
    CustomerSnapshotDto Customer,
    ProductSnapshotDto Product,
    IssuerSnapshotDto Issuer,
    CoverageSnapshotDto Coverage,
    SaleDetailsDto? SaleDetails,
    RepairDetailsDto? RepairDetails);
