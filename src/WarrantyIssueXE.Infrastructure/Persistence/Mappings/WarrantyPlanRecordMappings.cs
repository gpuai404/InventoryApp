using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Infrastructure.Persistence;

namespace WarrantyIssueXE.Infrastructure.Persistence.Mappings;

internal static partial class DomainRecordMappings
{
    public static WarrantyPlan ToDomain(this WarrantyPlanRecord record) =>
        new(
            ParseEnum<WarrantyType>(record.Type, nameof(WarrantyPlanRecord.Type)),
            record.Name,
            new WarrantyTerm(record.TermMonths),
            record.CoverageDescription,
            new WarrantyPlanId(record.Id));

    public static WarrantyPlanRecord ToRecord(this WarrantyPlan plan) =>
        new()
        {
            Id = plan.Id.Value,
            Type = plan.Type.ToString(),
            Name = plan.Name,
            TermMonths = plan.Term.Months,
            CoverageDescription = plan.CoverageDescription
        };
}
