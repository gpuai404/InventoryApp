using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Domain.WarrantyDetails;

namespace WarrantyIssueXE.Domain.Tests;

public sealed class WarrantyDetailsTests
{
    [Fact]
    public void Sale_and_repair_details_require_a_business_date()
    {
        var saleException = Assert.Throws<DomainValidationException>(
            () => new SaleDetails(default));
        var repairException = Assert.Throws<DomainValidationException>(
            () => new RepairDetails(default, "Replaced battery"));

        Assert.Equal(DomainErrorCodes.RequiredValue, saleException.Code);
        Assert.Equal("purchasedOn", saleException.PropertyName);
        Assert.Equal(DomainErrorCodes.RequiredValue, repairException.Code);
        Assert.Equal("repairedOn", repairException.PropertyName);
    }

    [Fact]
    public void Equal_sale_details_compare_equal_after_normalization()
    {
        var first = new SaleDetails(new DateOnly(2026, 1, 1));
        var second = new SaleDetails(new DateOnly(2026, 1, 1));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Repair_details_use_structural_equality_for_ordered_replaced_parts()
    {
        var first = new RepairDetails(
            new DateOnly(2026, 2, 5),
            "Replaced battery",
            [new ReplacedPartSnapshot("Battery", "BT-42")]);
        var equal = new RepairDetails(
            new DateOnly(2026, 2, 5),
            "Replaced battery",
            [new ReplacedPartSnapshot("Battery", "BT-42")]);
        var different = new RepairDetails(
            new DateOnly(2026, 2, 5),
            "Replaced battery",
            [new ReplacedPartSnapshot("Battery", "BT-43")]);

        Assert.Equal(first, equal);
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.True(first == equal);
        Assert.NotEqual(first, different);
    }

    [Fact]
    public void Repair_details_support_labor_only_work()
    {
        var details = new RepairDetails(new DateOnly(2026, 2, 5), "Diagnosed and repaired connector");

        Assert.Empty(details.ReplacedParts);
    }

    [Fact]
    public void Repair_details_require_a_repair_description()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new RepairDetails(new DateOnly(2026, 2, 5), " "));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
        Assert.Equal("workDescription", exception.PropertyName);
    }

    [Fact]
    public void Repair_details_copy_the_replaced_parts_collection()
    {
        var source = new List<ReplacedPartSnapshot>
        {
            new("Battery", "BT-42")
        };

        var details = new RepairDetails(new DateOnly(2026, 2, 5), "Replaced battery", source);
        source.Add(new ReplacedPartSnapshot("Connector"));

        Assert.Single(details.ReplacedParts);
    }
}
