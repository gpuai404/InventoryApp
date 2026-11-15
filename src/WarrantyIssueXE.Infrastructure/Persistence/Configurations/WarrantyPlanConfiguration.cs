using WarrantyIssueXE.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WarrantyIssueXE.Infrastructure.Persistence.Configurations;

internal sealed class WarrantyPlanConfiguration : IEntityTypeConfiguration<WarrantyPlanRecord>
{
    public void Configure(EntityTypeBuilder<WarrantyPlanRecord> entity)
    {
        entity.ToTable(
            "WarrantyPlans",
            table =>
            {
                table.HasCheckConstraint("CK_WarrantyPlans_TermMonths", "TermMonths > 0");
                table.HasCheckConstraint(
                    "CK_WarrantyPlans_Type",
                    "Type IN ('ProductSale', 'Repair')");
            });
        entity.HasKey(plan => plan.Id);
        entity.Property(plan => plan.Id).ValueGeneratedNever();
        entity.Property(plan => plan.Type).IsRequired().HasMaxLength(20);
        entity.Property(plan => plan.Name).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(plan => plan.CoverageDescription)
            .IsRequired()
            .HasMaxLength(DomainTextLimits.Description);
        ArchivableRecordConfiguration.Configure(entity);
    }
}
