namespace InventoryApp.Application.Abstractions.Persistence;

/// <summary>
/// Commits one application operation as a single persistence boundary.
/// The implementation belongs to Infrastructure (SQLite, an API-backed store,
/// or another persistence mechanism).
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
