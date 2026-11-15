using System.Reflection;
using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Domain.Tests;

public sealed class SnapshotTests
{
    [Fact]
    public void Equal_snapshot_values_compare_equal_after_normalization()
    {
        var first = new CustomerSnapshot(
            "  Ada Lovelace  ",
            "  ada@example.test  ",
            "  +32 2 555 0100  ",
            "  Avenue des Arts 1  ");
        var second = new CustomerSnapshot(
            "Ada Lovelace",
            "ada@example.test",
            "+32 2 555 0100",
            "Avenue des Arts 1");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Certificate_snapshots_expose_no_public_mutators()
    {
        var snapshotTypes = new[]
        {
            typeof(CustomerSnapshot),
            typeof(ProductSnapshot),
            typeof(IssuerSnapshot)
        };

        Assert.All(
            snapshotTypes.SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)),
            property => Assert.False(property.SetMethod?.IsPublic ?? false));
    }

    [Fact]
    public void Product_snapshot_contains_historical_identity_category_and_price()
    {
        var productId = ProductId.New();
        var categoryId = CategoryId.New();
        var snapshot = new ProductSnapshot(
            productId,
            "Laptop",
            categoryId,
            "Computers",
            new Money(899m, "EUR"));

        Assert.Equal(productId, snapshot.ProductId);
        Assert.Equal(categoryId, snapshot.CategoryId);
        Assert.Equal("Computers", snapshot.CategoryName);
        Assert.Equal(new Money(899m, "EUR"), snapshot.Price);
    }

    [Fact]
    public void Issuer_snapshot_requires_an_address()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new IssuerSnapshot("Contoso", null, " "));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
        Assert.Equal("address", exception.PropertyName);
    }
}
