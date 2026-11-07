using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.Primitives;

namespace InventoryApp.Domain.Entities;

/// <summary>
/// Base for objects that have identity and a lifetime in the domain.
///
/// Entities intentionally retain reference equality. Two instances with the same
/// identifier can represent different versions of the same persisted entity, so
/// the domain does not impose a state-based equality definition on them.
/// </summary>
public abstract class Entity<TId>
    where TId : struct, IEntityId
{
    protected Entity(TId id)
    {
        Id = DomainGuard.RequiredId(id, nameof(id));
    }

    public TId Id { get; }
}
