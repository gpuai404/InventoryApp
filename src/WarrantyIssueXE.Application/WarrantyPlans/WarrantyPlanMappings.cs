using WarrantyIssueXE.Domain.Entities;

namespace WarrantyIssueXE.Application.WarrantyPlans;

internal static class WarrantyPlanMappings
{
    public static WarrantyPlanDto ToDto(this WarrantyPlan plan) =>
        new(
            plan.Id.Value,
            plan.Type,
            plan.Name,
            plan.Term.Months,
            plan.CoverageDescription);
}
