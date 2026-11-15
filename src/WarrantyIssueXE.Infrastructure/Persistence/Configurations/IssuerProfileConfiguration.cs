using WarrantyIssueXE.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WarrantyIssueXE.Infrastructure.Persistence.Configurations;

internal sealed class IssuerProfileConfiguration : IEntityTypeConfiguration<IssuerProfileRecord>
{
    public void Configure(EntityTypeBuilder<IssuerProfileRecord> entity)
    {
        entity.ToTable(
            "IssuerProfile",
            table => table.HasCheckConstraint("CK_IssuerProfile_Singleton", "Id = 1"));
        entity.HasKey(profile => profile.Id);
        entity.Property(profile => profile.Id).ValueGeneratedNever();
        entity.Property(profile => profile.Name).IsRequired().HasMaxLength(DomainTextLimits.Name);
        entity.Property(profile => profile.VatNumber).HasMaxLength(DomainTextLimits.VatNumber);
        entity.Property(profile => profile.Address).HasMaxLength(DomainTextLimits.ContactAddress);
        entity.Property(profile => profile.Email).HasMaxLength(DomainTextLimits.ContactEmail);
        entity.Property(profile => profile.Phone).HasMaxLength(DomainTextLimits.ContactPhone);
        entity.Property(profile => profile.UpdatedAtUtc).IsRequired();
    }
}
