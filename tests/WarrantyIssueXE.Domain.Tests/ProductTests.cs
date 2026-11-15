using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Product_rejects_an_empty_name()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new Product(CategoryId.New(), " ", new Money(1m, "EUR")));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
        Assert.Equal("name", exception.PropertyName);
    }

    [Fact]
    public void Product_rejects_a_missing_price()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new Product(CategoryId.New(), "Laptop", null!));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
        Assert.Equal("price", exception.PropertyName);
    }

    [Fact]
    public void Product_revision_preserves_identity()
    {
        var product = new Product(CategoryId.New(), "Laptop", new Money(899m, "EUR"));

        var revised = product.Revise(
            product.CategoryId,
            "Laptop Pro",
            new Money(999m, "EUR"),
            "Revised model");

        Assert.Equal(product.Id, revised.Id);
        Assert.Equal("Laptop", product.Name);
        Assert.Equal("Laptop Pro", revised.Name);
    }

    [Fact]
    public void Product_price_changes_do_not_alter_a_previously_issued_snapshot()
    {
        var product = new Product(CategoryId.New(), "Laptop", new Money(899m, "EUR"));
        var snapshot = product.ToSnapshot("Computers");

        var revised = product.Revise(
            product.CategoryId,
            product.Name,
            new Money(999m, "EUR"),
            product.Description);

        Assert.Equal(product.Id, snapshot.ProductId);
        Assert.Equal(product.CategoryId, snapshot.CategoryId);
        Assert.Equal("Computers", snapshot.CategoryName);
        Assert.Equal(new Money(899m, "EUR"), snapshot.Price);
        Assert.Equal(new Money(999m, "EUR"), revised.Price);
    }
}
