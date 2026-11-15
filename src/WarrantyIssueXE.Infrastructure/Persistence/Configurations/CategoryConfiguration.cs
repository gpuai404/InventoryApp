using WarrantyIssueXE.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WarrantyIssueXE.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<CategoryRecord>
{
    public void Configure(EntityTypeBuilder<CategoryRecord> entity)
    {
        entity.ToTable("Categories");
        entity.HasKey(category => category.Id);
        entity.Property(category => category.Id).ValueGeneratedNever();
        entity.Property(category => category.Name).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(category => category.Description).HasMaxLength(DomainTextLimits.Description);
        ArchivableRecordConfiguration.Configure(entity);
    }
}
