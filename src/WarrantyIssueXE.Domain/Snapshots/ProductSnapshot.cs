using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.Primitives;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Domain.Snapshots;

/// <summary>
/// Product facts as they appeared when a warranty was issued. The category
/// name and price are historical values, not live catalog references.
/// </summary>
public sealed record ProductSnapshot
{
    public ProductSnapshot(
        ProductId productId,
        string productName,
        CategoryId categoryId,
        string categoryName,
        Money price)
    {
        ProductId = DomainGuard.RequiredId(productId, nameof(productId));
        ProductName = DomainGuard.RequiredText(productName, nameof(productName), DomainTextLimits.Name);
        CategoryId = DomainGuard.RequiredId(categoryId, nameof(categoryId));
        CategoryName = DomainGuard.RequiredText(categoryName, nameof(categoryName), DomainTextLimits.Name);
        Price = DomainGuard.Required(price, nameof(price));
    }

    public ProductId ProductId { get; }

    public string ProductName { get; }

    public CategoryId CategoryId { get; }

    public string CategoryName { get; }

    public Money Price { get; }
}
