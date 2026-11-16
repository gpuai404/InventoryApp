using WarrantyIssueXE.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WarrantyIssueXE.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<ProductRecord>
{
    public void Configure(EntityTypeBuilder<ProductRecord> entity)
    {
        entity.ToTable(
            "Products",
            table => table.HasCheckConstraint(
                "CK_Products_PriceAmount",
                "CAST(PriceAmount AS NUMERIC) >= 0"));
        entity.HasKey(product => product.Id);
        entity.Property(product => product.Id).ValueGeneratedNever();
        entity.Property(product => product.CategoryId).IsRequired();
        entity.Property(product => product.Name).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(product => product.PriceAmount).IsRequired();
        entity.Property(product => product.PriceCurrencyCode)
            .IsRequired()
            .HasMaxLength(DomainTextLimits.CurrencyCode);
        entity.Property(product => product.Description).HasMaxLength(DomainTextLimits.Description);
        entity.HasOne<CategoryRecord>()
            .WithMany()
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        ArchivableRecordConfiguration.Configure(entity);
    }
}
