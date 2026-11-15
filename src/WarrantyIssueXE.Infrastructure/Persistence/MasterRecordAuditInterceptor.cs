using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WarrantyIssueXE.Infrastructure.Persistence;

/// <summary>
/// Stamps persistence-only audit data for changed master records.
/// </summary>
internal sealed class MasterRecordAuditInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var entry in context.ChangeTracker.Entries<ArchivableRecord>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(record => record.UpdatedAtUtc).CurrentValue = now;
                entry.Property(record => record.UpdatedAtUtc).IsModified = true;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<IssuerProfileRecord>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                entry.Entity.UpdatedAtUtc = now;
                continue;
            }

            entry.Property(profile => profile.UpdatedAtUtc).CurrentValue = now;
            entry.Property(profile => profile.UpdatedAtUtc).IsModified = true;
        }
    }
}
