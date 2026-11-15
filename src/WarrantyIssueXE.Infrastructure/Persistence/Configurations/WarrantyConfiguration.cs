using WarrantyIssueXE.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
                table.HasCheckConstraint(
                    "CK_Warranties_TypeAndStatus",
                    "Type IN ('ProductSale', 'Repair') AND Status IN ('Issued', 'Voided')");
                table.HasCheckConstraint(
                    "CK_Warranties_Lifecycle",
                    "(Status = 'Issued' AND VoidedAtUtcTicks IS NULL AND VoidReason IS NULL) OR " +
                    "(Status = 'Voided' AND VoidedAtUtcTicks IS NOT NULL AND VoidReason IS NOT NULL)");
                table.HasCheckConstraint(
                    "CK_Warranties_VoidTimestamp",
                    "VoidedAtUtcTicks IS NULL OR VoidedAtUtcTicks >= IssuedAtUtcTicks");
                table.HasCheckConstraint(
                    "CK_Warranties_TypeSpecificDetails",
                    "(Type = 'ProductSale' AND PurchasedOn IS NOT NULL " +
                    "AND PurchasePriceAmount IS NOT NULL AND PurchasePriceAmount >= 0 " +
                    "AND PurchasePriceCurrencyCode IS NOT NULL " +
                    "AND RepairedOn IS NULL AND RepairWorkDescription IS NULL " +
                    "AND StartsOn = PurchasedOn) OR " +
                    "(Type = 'Repair' AND PurchasedOn IS NULL " +
                    "AND PurchasePriceAmount IS NULL AND PurchasePriceCurrencyCode IS NULL " +
                    "AND RepairedOn IS NOT NULL AND RepairWorkDescription IS NOT NULL " +
                    "AND StartsOn = RepairedOn)");
                table.HasCheckConstraint(
                    "CK_Warranties_CoverageScope",
                    "(Type = 'ProductSale' AND CoverageScope = 'Product') OR " +
                    "(Type = 'Repair' AND CoverageScope = 'RepairWorkAndReplacedParts')");
            });
        entity.HasKey(warranty => warranty.Id);
        entity.Property(warranty => warranty.Id).ValueGeneratedNever();
        entity.Property(warranty => warranty.CertificateNumber)
            .IsRequired()
            .HasMaxLength(DomainTextLimits.WarrantyCertificateNumber);
        entity.Property(warranty => warranty.Type).IsRequired().HasMaxLength(20);
        entity.Property(warranty => warranty.Status)
            .IsRequired()
            .HasMaxLength(20)
            .IsConcurrencyToken();
        entity.Property(warranty => warranty.SerialNumber).HasMaxLength(DomainTextLimits.WarrantySerialNumber);
        entity.Property(warranty => warranty.VoidReason).HasMaxLength(DomainTextLimits.Description);
        entity.Property(warranty => warranty.CustomerFullName).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(warranty => warranty.CustomerEmail).HasMaxLength(DomainTextLimits.ContactEmail);
        entity.Property(warranty => warranty.CustomerPhone).HasMaxLength(DomainTextLimits.ContactPhone);
        entity.Property(warranty => warranty.CustomerAddress).HasMaxLength(DomainTextLimits.ContactAddress);
        entity.Property(warranty => warranty.ProductName).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(warranty => warranty.ProductVariantName).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(warranty => warranty.ProductSku).HasMaxLength(DomainTextLimits.ProductVariantSku);
        entity.Property(warranty => warranty.IssuerName).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(warranty => warranty.IssuerVatNumber).HasMaxLength(DomainTextLimits.VatNumber);
        entity.Property(warranty => warranty.IssuerAddress).HasMaxLength(DomainTextLimits.ContactAddress);
        entity.Property(warranty => warranty.IssuerEmail).HasMaxLength(DomainTextLimits.ContactEmail);
        entity.Property(warranty => warranty.IssuerPhone).HasMaxLength(DomainTextLimits.ContactPhone);
        entity.Property(warranty => warranty.CoverageScope).IsRequired().HasMaxLength(40);
        entity.Property(warranty => warranty.CoverageDescription)
            .IsRequired()
            .HasMaxLength(DomainTextLimits.Description);
        entity.Property(warranty => warranty.CoverageScopeStatement)
            .IsRequired()
            .HasMaxLength(DomainTextLimits.WarrantyScopeStatement);
        entity.Property(warranty => warranty.PurchasePriceCurrencyCode)
            .HasMaxLength(DomainTextLimits.CurrencyCode);
        entity.Property(warranty => warranty.RepairWorkDescription)
            .HasMaxLength(DomainTextLimits.Description);
        entity.HasIndex(warranty => warranty.CertificateNumber).IsUnique();
        entity.HasIndex(warranty => new { warranty.Status, warranty.IssuedAtUtcTicks });
        entity.HasIndex(warranty => warranty.SerialNumber);
        entity.HasOne<ProductVariantRecord>()
            .WithMany()
            .HasForeignKey(warranty => warranty.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<WarrantyPlanRecord>()
            .WithMany()
            .HasForeignKey(warranty => warranty.WarrantyPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(warranty => warranty.ReplacedParts)
            .WithOne()
            .HasForeignKey(part => part.WarrantyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
