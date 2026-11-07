namespace InventoryApp.Application.Abstractions.Persistence;

/// <summary>
/// Shared read-side shape for an aggregate-specific persistence port.
/// Concrete aggregate ports add any queries that are meaningful for that
/// aggregate.
/// </summary>
public interface IEntityReader<TEntity, in TId>
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default);
}
