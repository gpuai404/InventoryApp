using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Application.Common;

/// <summary>
/// Makes a required aggregate lookup explicit at an application boundary.
/// </summary>
internal static class EntityReaderExtensions
{
    public static async Task<TEntity> GetRequiredAsync<TEntity, TId>(
        this IEntityReader<TEntity, TId> reader,
        TId id,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : struct, IEntityId
    {
        ArgumentNullException.ThrowIfNull(reader);

        return await reader.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(TEntity).Name, id.Value);
    }
}
