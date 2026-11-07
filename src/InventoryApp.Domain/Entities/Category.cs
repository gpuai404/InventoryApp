using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.Primitives;

namespace InventoryApp.Domain.Entities;

public sealed class Category : Entity<CategoryId>
{
    public Category(string name, string? description = null, CategoryId? id = null)
        : base(id ?? CategoryId.New())
    {
        Name = DomainGuard.RequiredText(name, nameof(name), DomainTextLimits.Name);
        Description = DomainGuard.OptionalText(
            description,
            nameof(description),
            DomainTextLimits.Description);
    }

    public string Name { get; private init; }

    public string? Description { get; private init; }

    /// <summary>
    /// Replaces the category's editable state while preserving its identity.
    /// </summary>
    public Category Revise(string name, string? description) =>
        new(name, description, Id);
}
