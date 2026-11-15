using WarrantyIssueXE.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WarrantyIssueXE.Infrastructure.Persistence.Configurations;

internal sealed class WarrantyReplacedPartConfiguration : IEntityTypeConfiguration<WarrantyReplacedPartRecord>
{
    public void Configure(EntityTypeBuilder<WarrantyReplacedPartRecord> entity)
    {
        entity.ToTable("WarrantyReplacedParts");
        entity.HasKey(part => new { part.WarrantyId, part.Position });
        entity.Property(part => part.Description)
            .IsRequired()
            .HasMaxLength(DomainTextLimits.Description);
        entity.Property(part => part.PartNumber).HasMaxLength(DomainTextLimits.ReplacedPartNumber);
    }
}
