using InventoryApp.Domain.Exceptions;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Domain.Tests;

public sealed class EntityIdTests
{
    [Fact]
    public void Empty_typed_identifier_is_rejected()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new ProductId(Guid.Empty));

        Assert.Equal(DomainErrorCodes.EmptyIdentifier, exception.Code);
        Assert.Equal("value", exception.PropertyName);
    }

    [Fact]
    public void Constructing_an_identifier_from_its_value_preserves_identity()
    {
        var id = ProductVariantId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
        Assert.Equal(id, new ProductVariantId(id.Value));
    }

    [Fact]
    public void Entities_with_the_same_identifier_are_not_the_same_object()
    {
        var productId = ProductId.New();

        var categoryId = CategoryId.New();
        var first = new Product(categoryId, "Laptop", new Money(100m, "EUR"), id: productId);
        var second = new Product(categoryId, "Laptop", new Money(100m, "EUR"), id: productId);

        Assert.Equal(productId, first.Id);
        Assert.Equal(productId, second.Id);
        Assert.NotSame(first, second);
        Assert.NotEqual(first, second);
    }
}
