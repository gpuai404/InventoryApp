using WarrantyIssueXE.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WarrantyIssueXE.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariantRecord>
{
    public void Configure(EntityTypeBuilder<ProductVariantRecord> entity)
    {
        entity.ToTable("ProductVariants");
        entity.HasKey(variant => variant.Id);
        entity.Property(variant => variant.Id).ValueGeneratedNever();
        entity.Property(variant => variant.Name).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(variant => variant.Sku).HasMaxLength(DomainTextLimits.ProductVariantSku);
        entity.HasOne<ProductRecord>()
            .WithMany()
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(variant => variant.Barcodes)
            .WithOne()
            .HasForeignKey(barcode => barcode.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
        ArchivableRecordConfiguration.Configure(entity);
    }
}
