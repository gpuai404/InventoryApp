using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.Primitives;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Domain.Entities;

public sealed class Product : Entity<ProductId>
{
    public Product(
        CategoryId categoryId,
        string name,
        Money price,
        string? description = null,
        ProductId? id = null)
        : base(id ?? ProductId.New())
    {
        CategoryId = DomainGuard.RequiredId(categoryId, nameof(categoryId));
        Name = DomainGuard.RequiredText(name, nameof(name), DomainTextLimits.Name);
        Price = DomainGuard.Required(price, nameof(price));
        Description = DomainGuard.OptionalText(
            description,
            nameof(description),
            DomainTextLimits.Description);
    }

    /// <summary>
    /// The single category used to classify this product.
    /// </summary>
    public CategoryId CategoryId { get; private init; }

    public string Name { get; private init; }

    public Money Price { get; private init; }

    public string? Description { get; private init; }

    /// <summary>
    /// Replaces the product's editable state while preserving its identity.
    /// </summary>
    public Product Revise(
        CategoryId categoryId,
        string name,
        Money price,
        string? description) =>
        new(categoryId, name, price, description, Id);

    /// <summary>
    /// Captures the catalog facts needed by an issued certificate. Category
    /// display text is supplied by the category catalog and frozen here.
    /// </summary>
    public ProductSnapshot ToSnapshot(string categoryName) =>
        new(Id, Name, CategoryId, categoryName, Price);
}
