namespace WarrantyIssueXE.Application.Common;

/// <summary>
/// Converts an optional aggregate lookup into the application-level not-found
/// result used by command workflows.
/// </summary>
internal static class RequiredEntityExtensions
{
    public static async Task<TEntity> RequireFoundAsync<TEntity>(
        this Task<TEntity?> lookup,
        Guid id)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(lookup);

        return await lookup
            ?? throw new EntityNotFoundException(typeof(TEntity).Name, id);
    }
}
