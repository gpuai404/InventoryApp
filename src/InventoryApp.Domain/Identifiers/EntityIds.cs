using InventoryApp.Domain.Primitives;

namespace InventoryApp.Domain.Identifiers;

/// <summary>
/// Marker for domain identifiers backed by a Guid.
/// The concrete type prevents accidentally passing one entity's identifier as another's.
/// </summary>
public interface IEntityId
{
    Guid Value { get; }
}

public readonly record struct CategoryId : IEntityId
{
    public CategoryId(Guid value)
    {
        Value = DomainGuard.RequiredId(value, nameof(value));
    }

    public Guid Value { get; }

    public static CategoryId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

public readonly record struct CompanyId : IEntityId
{
    public CompanyId(Guid value)
    {
        Value = DomainGuard.RequiredId(value, nameof(value));
    }

    public Guid Value { get; }

    public static CompanyId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

public readonly record struct ProductId : IEntityId
{
    public ProductId(Guid value)
    {
        Value = DomainGuard.RequiredId(value, nameof(value));
    }

    public Guid Value { get; }

    public static ProductId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

public readonly record struct ProductVariantId : IEntityId
{
    public ProductVariantId(Guid value)
    {
        Value = DomainGuard.RequiredId(value, nameof(value));
    }

    public Guid Value { get; }

    public static ProductVariantId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

public readonly record struct ServiceId : IEntityId
{
    public ServiceId(Guid value)
    {
        Value = DomainGuard.RequiredId(value, nameof(value));
    }

    public Guid Value { get; }

    public static ServiceId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

public readonly record struct WarrantyId : IEntityId
{
    public WarrantyId(Guid value)
    {
        Value = DomainGuard.RequiredId(value, nameof(value));
    }

    public Guid Value { get; }

    public static WarrantyId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}
