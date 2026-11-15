using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WarrantyIssueXE.Infrastructure.Persistence.Configurations;

internal static class ArchivableRecordConfiguration
{
    public static void Configure<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : ArchivableRecord
    {
        entity.Property(record => record.CreatedAtUtc).IsRequired();
        entity.Property(record => record.UpdatedAtUtc).IsRequired();
        entity.Property(record => record.ArchivedAtUtc);
        entity.HasIndex(record => record.ArchivedAtUtc);
        entity.HasQueryFilter(record => record.ArchivedAtUtc == null);
    }
}
