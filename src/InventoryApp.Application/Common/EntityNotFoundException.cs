namespace InventoryApp.Application.Common;

/// <summary>
/// Signals that an application operation could not find the aggregate it needs.
/// Presentation layers can map this to a not-found result without knowing the
/// persistence implementation.
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string entityName, Guid id)
        : base($"{entityName} with identifier '{id:D}' was not found.")
    {
        EntityName = entityName;
        Id = id;
    }

    public string EntityName { get; }

    public Guid Id { get; }
}
