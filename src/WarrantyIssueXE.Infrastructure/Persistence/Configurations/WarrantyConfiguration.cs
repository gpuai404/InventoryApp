using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarrantyIssueXE.Domain.Primitives;

namespace WarrantyIssueXE.Infrastructure.Persistence.Configurations;

internal sealed class WarrantyConfiguration : IEntityTypeConfiguration<WarrantyRecord>
{
    public void Configure(EntityTypeBuilder<WarrantyRecord> entity)
    {
        entity.ToTable(
            "Warranties",
            table =>
            {
                table.HasCheckConstraint("CK_Warranties_DateRange", "ExpiresOn >= StartsOn");
                table.HasCheckConstraint("CK_Warranties_CoverageTerm", "CoverageTermMonths > 0");
                table.HasCheckConstraint("CK_Warranties_ProductPrice", "CAST(ProductPriceAmount AS NUMERIC) >= 0");
                table.HasCheckConstraint("CK_Warranties_Type", "Type IN ('ProductSale', 'Repair')");
                table.HasCheckConstraint(
                    "CK_Warranties_TypeSpecificDetails",
                    "(Type = 'ProductSale' AND PurchasedOn IS NOT NULL " +
                    "AND RepairedOn IS NULL AND RepairWorkDescription IS NULL " +
                    "AND StartsOn = PurchasedOn) OR " +
                    "(Type = 'Repair' AND PurchasedOn IS NULL " +
                    "AND RepairedOn IS NOT NULL AND RepairWorkDescription IS NOT NULL " +
                    "AND StartsOn = RepairedOn)");
                table.HasCheckConstraint(
                    "CK_Warranties_CoverageScope",
                    "(Type = 'ProductSale' AND CoverageScope = 'Product') OR " +
                    "(Type = 'Repair' AND CoverageScope = 'RepairWorkAndReplacedParts')");
                table.HasCheckConstraint(
                    "CK_Warranties_Imei",
                    "Imei IS NULL OR (length(Imei) = 15 AND Imei NOT GLOB '*[^0-9]*')");
            });

        entity.HasKey(warranty => warranty.Id);
        entity.Property(warranty => warranty.Id).ValueGeneratedNever();
        entity.Property(warranty => warranty.CertificateNumber)
            .IsRequired()
            .HasMaxLength(DomainTextLimits.WarrantyCertificateNumber);
        entity.Property(warranty => warranty.Type).IsRequired().HasMaxLength(20);
        entity.Property(warranty => warranty.Imei).HasMaxLength(DomainTextLimits.Imei);
        entity.Property(warranty => warranty.CustomerFullName).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(warranty => warranty.CustomerEmail).HasMaxLength(DomainTextLimits.ContactEmail);
        entity.Property(warranty => warranty.CustomerPhone).HasMaxLength(DomainTextLimits.ContactPhone);
        entity.Property(warranty => warranty.CustomerAddress).HasMaxLength(DomainTextLimits.ContactAddress);
        entity.Property(warranty => warranty.ProductName).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(warranty => warranty.ProductCategoryName).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(warranty => warranty.ProductPriceAmount).IsRequired();
        entity.Property(warranty => warranty.ProductPriceCurrencyCode)
            .IsRequired()
            .HasMaxLength(DomainTextLimits.CurrencyCode);
        entity.Property(warranty => warranty.IssuerName).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(warranty => warranty.IssuerVatNumber).HasMaxLength(DomainTextLimits.VatNumber);
        entity.Property(warranty => warranty.IssuerAddress)
            .IsRequired()
            .HasMaxLength(DomainTextLimits.ContactAddress);
        entity.Property(warranty => warranty.IssuerEmail).HasMaxLength(DomainTextLimits.ContactEmail);
        entity.Property(warranty => warranty.IssuerPhone).HasMaxLength(DomainTextLimits.ContactPhone);
        entity.Property(warranty => warranty.CoverageScope).IsRequired().HasMaxLength(40);
        entity.Property(warranty => warranty.CoverageDescription)
            .IsRequired()
            .HasMaxLength(DomainTextLimits.Description);
        entity.Property(warranty => warranty.CoverageScopeStatement)
            .IsRequired()
            .HasMaxLength(DomainTextLimits.WarrantyScopeStatement);
        entity.Property(warranty => warranty.RepairWorkDescription)
            .HasMaxLength(DomainTextLimits.Description);

        entity.HasIndex(warranty => warranty.CertificateNumber).IsUnique();
        entity.HasIndex(warranty => warranty.IssuedAtUtcTicks);
        entity.HasMany(warranty => warranty.ReplacedParts)
            .WithOne()
            .HasForeignKey(part => part.WarrantyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
