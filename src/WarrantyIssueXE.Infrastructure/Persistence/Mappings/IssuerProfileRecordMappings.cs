using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Infrastructure.Persistence;

namespace WarrantyIssueXE.Infrastructure.Persistence.Mappings;

internal static partial class DomainRecordMappings
{
    public static IssuerProfile ToDomain(this IssuerProfileRecord record) =>
        new(record.Name, record.VatNumber, record.Address, record.Email, record.Phone);

    public static IssuerProfileRecord ToRecord(this IssuerProfile profile) =>
        new()
        {
            Id = IssuerProfileRecord.SingletonId,
            Name = profile.Name,
            VatNumber = profile.VatNumber,
            Address = profile.Address,
            Email = profile.Email,
            Phone = profile.Phone
        };
}
