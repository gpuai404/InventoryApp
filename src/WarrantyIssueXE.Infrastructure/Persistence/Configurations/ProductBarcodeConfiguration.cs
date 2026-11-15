using WarrantyIssueXE.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WarrantyIssueXE.Infrastructure.Persistence.Configurations;

internal sealed class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcodeRecord>
{
    public void Configure(EntityTypeBuilder<ProductBarcodeRecord> entity)
    {
        entity.ToTable("ProductBarcodes");
        entity.HasKey(barcode => barcode.Id);
        entity.Property(barcode => barcode.Id).ValueGeneratedNever();
        entity.Property(barcode => barcode.Code)
            .IsRequired()
            .HasMaxLength(DomainTextLimits.ProductBarcodeCode);
        entity.Property(barcode => barcode.PackagingDescription)
            .HasMaxLength(DomainTextLimits.ProductBarcodePackagingDescription);
        entity.HasIndex(barcode => barcode.Code).IsUnique();
    }
}
