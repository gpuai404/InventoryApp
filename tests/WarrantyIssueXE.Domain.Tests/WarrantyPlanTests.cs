using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Domain.Tests;

public sealed class WarrantyPlanTests
{
    [Fact]
    public void Warranty_plan_revision_preserves_identity_and_workflow()
    {
        var plan = new WarrantyPlan(WarrantyType.ProductSale, "Standard", new WarrantyTerm(24), "Parts and labour");
        var revised = plan.Revise("Extended", new WarrantyTerm(36), "Extended parts and labour");

        Assert.Equal(plan.Id, revised.Id);
        Assert.Equal(WarrantyType.ProductSale, revised.Type);
    }

    [Theory]
    [InlineData(WarrantyType.ProductSale, WarrantyType.Repair)]
    [InlineData(WarrantyType.Repair, WarrantyType.ProductSale)]
    public void Warranty_plan_cannot_create_coverage_for_another_workflow(
        WarrantyType planType,
        WarrantyType requestedType)
    {
        var plan = new WarrantyPlan(planType, "Standard", new WarrantyTerm(12), "Agreed terms");

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => plan.CreateCoverageFor(requestedType));

        Assert.Equal(DomainErrorCodes.WarrantyPlanTypeMismatch, exception.Code);
    }
}
