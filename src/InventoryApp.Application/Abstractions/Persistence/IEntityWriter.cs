namespace InventoryApp.Application.Abstractions.Persistence;

/// <summary>
/// Shared write-side shape for an aggregate-specific persistence port.
/// The application passes complete immutable replacements to UpdateAsync.
/// </summary>
public interface IEntityWriter<TEntity, in TId>
    where TEntity : class
{
    Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hides an aggregate from normal operations while preserving its persistence history.
    /// </summary>
    Task ArchiveAsync(
        TId id,
        CancellationToken cancellationToken = default);
}
