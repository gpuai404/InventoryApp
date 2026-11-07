using InventoryApp.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApp.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    internal DbSet<CategoryRecord> Categories => Set<CategoryRecord>();

    internal DbSet<CompanyRecord> Companies => Set<CompanyRecord>();

    internal DbSet<ProductRecord> Products => Set<ProductRecord>();

    internal DbSet<ProductVariantRecord> ProductVariants => Set<ProductVariantRecord>();

    internal DbSet<ServiceRecord> Services => Set<ServiceRecord>();

    internal DbSet<WarrantyRecord> Warranties => Set<WarrantyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoryRecord>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).ValueGeneratedNever();
            entity.Property(category => category.Name).IsRequired().HasMaxLength(DomainTextLimits.Name);
            entity.Property(category => category.Description).HasMaxLength(DomainTextLimits.Description);
            ConfigureSoftDeletion(entity);
        });

        modelBuilder.Entity<CompanyRecord>(entity =>
        {
            entity.ToTable("Companies");
            entity.HasKey(company => company.Id);
            entity.Property(company => company.Id).ValueGeneratedNever();
            entity.Property(company => company.Name).IsRequired().HasMaxLength(DomainTextLimits.Name);
            entity.Property(company => company.Address).HasMaxLength(DomainTextLimits.CompanyAddress);
            entity.Property(company => company.Email).HasMaxLength(DomainTextLimits.CompanyEmail);
            entity.Property(company => company.Phone).HasMaxLength(DomainTextLimits.CompanyPhone);
            ConfigureSoftDeletion(entity);
        });

        modelBuilder.Entity<ProductRecord>(entity =>
        {
            entity.ToTable("Products");
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
            ConfigureSoftDeletion(entity);
        });

        modelBuilder.Entity<ProductVariantRecord>(entity =>
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
            ConfigureSoftDeletion(entity);
        });

        modelBuilder.Entity<ProductBarcodeRecord>(entity =>
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
        });

        modelBuilder.Entity<ServiceRecord>(entity =>
        {
            entity.ToTable(
                "Services",
                table => table.HasCheckConstraint(
                    "CK_Services_WarrantyTermMonths",
                    "WarrantyTermMonths > 0"));
            entity.HasKey(service => service.Id);
            entity.Property(service => service.Id).ValueGeneratedNever();
            entity.Property(service => service.Name).IsRequired().HasMaxLength(DomainTextLimits.Name);
            entity.Property(service => service.WarrantyTermMonths).IsRequired();
            entity.Property(service => service.Description).HasMaxLength(DomainTextLimits.Description);
            entity.HasOne<CompanyRecord>()
                .WithMany()
                .HasForeignKey(service => service.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            ConfigureSoftDeletion(entity);
        });

        modelBuilder.Entity<WarrantyRecord>(entity =>
        {
            entity.ToTable("Warranties");
            entity.HasKey(warranty => warranty.Id);
            entity.Property(warranty => warranty.Id).ValueGeneratedNever();
            entity.Property(warranty => warranty.ProductId).IsRequired();
            entity.Property(warranty => warranty.ServiceId).IsRequired();
            entity.Property(warranty => warranty.Description)
                .IsRequired()
                .HasMaxLength(DomainTextLimits.Description);
            entity.Property(warranty => warranty.StartsOn).IsRequired();
            entity.Property(warranty => warranty.ExpiresOn).IsRequired();
            ConfigureSoftDeletion(entity);

            entity.HasOne<ProductRecord>()
                .WithMany()
                .HasForeignKey(warranty => warranty.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ServiceRecord>()
                .WithMany()
                .HasForeignKey(warranty => warranty.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSoftDeletion<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : SoftDeletableRecord
    {
        entity.Property(record => record.DeletedOnUtc);
        entity.HasIndex(record => record.DeletedOnUtc);
        entity.HasQueryFilter(record => record.DeletedOnUtc == null);
    }
}
