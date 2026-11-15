using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.WarrantyDetails;

namespace WarrantyIssueXE.Application.Warranties;

internal static class WarrantyMappings
{
    public static WarrantyDto ToDto(this Warranty warranty) =>
        new(
            warranty.Id.Value,
            warranty.CertificateNumber,
            warranty.Type,
            warranty.Status,
            warranty.ProductVariantId.Value,
            warranty.WarrantyPlanId?.Value,
            warranty.SerialNumber,
            warranty.IssuedAt,
            warranty.StartsOn,
            warranty.ExpiresOn,
            warranty.Customer.ToDto(),
            warranty.Product.ToDto(),
            warranty.Issuer.ToDto(),
            warranty.Coverage.ToDto(),
            warranty.SaleDetails?.ToDto(),
            warranty.RepairDetails?.ToDto(),
            warranty.VoidedAt,
            warranty.VoidReason);

    private static CustomerSnapshotDto ToDto(this CustomerSnapshot customer) =>
        new(customer.FullName, customer.Email, customer.Phone, customer.Address);

    private static ProductSnapshotDto ToDto(this ProductSnapshot product) =>
        new(product.ProductName, product.VariantName, product.Sku);

    private static IssuerSnapshotDto ToDto(this IssuerSnapshot issuer) =>
        new(issuer.Name, issuer.VatNumber, issuer.Address, issuer.Email, issuer.Phone);

    private static CoverageSnapshotDto ToDto(this CoverageSnapshot coverage) =>
        new(
            coverage.Term.Months,
            coverage.Scope,
            coverage.CoverageDescription,
            coverage.ScopeStatement);

    private static SaleDetailsDto ToDto(this SaleDetails sale) =>
        new(sale.PurchasedOn, sale.PurchasePrice.Amount, sale.PurchasePrice.CurrencyCode);

    private static RepairDetailsDto ToDto(this RepairDetails repair) =>
        new(
            repair.RepairedOn,
            repair.WorkDescription,
            repair.ReplacedParts.Select(part => new ReplacedPartSnapshotDto(part.Description, part.PartNumber)).ToArray());
}
