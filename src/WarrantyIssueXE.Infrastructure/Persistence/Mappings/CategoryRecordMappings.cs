using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Infrastructure.Persistence;

namespace WarrantyIssueXE.Infrastructure.Persistence.Mappings;

internal static partial class DomainRecordMappings
{
    public static Category ToDomain(this CategoryRecord record) =>
        new(record.Name, record.Description, new CategoryId(record.Id));

    public static CategoryRecord ToRecord(this Category category) =>
        new()
        {
            Id = category.Id.Value,
            Name = category.Name,
            Description = category.Description
        };
}
