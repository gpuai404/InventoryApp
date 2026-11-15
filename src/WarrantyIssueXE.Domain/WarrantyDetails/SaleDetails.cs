using WarrantyIssueXE.Domain.Primitives;

namespace WarrantyIssueXE.Domain.WarrantyDetails;

/// <summary>
/// The completed-sale facts captured on a product-sale certificate.
/// </summary>
public sealed record SaleDetails
{
    public SaleDetails(DateOnly purchasedOn)
    {
        PurchasedOn = DomainGuard.RequiredDate(purchasedOn, nameof(purchasedOn));
    }

    public DateOnly PurchasedOn { get; }
}
