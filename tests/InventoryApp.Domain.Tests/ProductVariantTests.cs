using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Exceptions;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Domain.Tests;

public sealed class ProductVariantTests
{
    [Fact]
    public void Variant_references_product_by_id_and_owns_barcode_values()
    {
        var productId = ProductId.New();
        var barcode = new ProductBarcode(" 123 ", " Box ");

        var variant = new ProductVariant(productId, "Standard", barcodes: [barcode]);

        Assert.Equal(productId, variant.ProductId);
        Assert.Equal("123", variant.Barcodes.Single().Code);
        Assert.Equal("Box", variant.Barcodes.Single().PackagingDescription);
    }

    [Fact]
    public void Variant_copies_the_input_barcode_collection()
    {
        var source = new List<ProductBarcode>
        {
            new("123")
        };

        var variant = new ProductVariant(ProductId.New(), "Standard", barcodes: source);

        source.Add(new ProductBarcode("456"));

        Assert.Single(variant.Barcodes);
        Assert.IsAssignableFrom<IReadOnlyList<ProductBarcode>>(variant.Barcodes);
    }

    [Fact]
    public void Add_barcode_returns_new_version_without_mutating_original()
    {
        var variant = new ProductVariant(ProductId.New(), "Standard");
        var updated = variant.AddBarcode(new ProductBarcode("123"));

        Assert.Equal(variant.Id, updated.Id);
        Assert.Empty(variant.Barcodes);
        Assert.Single(updated.Barcodes);
    }

    [Fact]
    public void Duplicate_barcode_is_rejected()
    {
        var variant = new ProductVariant(
            ProductId.New(),
            "Standard",
            barcodes: [new ProductBarcode("123")]);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => variant.AddBarcode(new ProductBarcode("123")));

        Assert.Equal(DomainErrorCodes.DuplicateBarcode, exception.Code);
    }

    [Fact]
    public void Duplicate_barcode_is_rejected_after_code_normalization()
    {
        var variant = new ProductVariant(
            ProductId.New(),
            "Standard",
            barcodes: [new ProductBarcode("123")]);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => variant.AddBarcode(new ProductBarcode(" 123 ")));

        Assert.Equal(DomainErrorCodes.DuplicateBarcode, exception.Code);
    }

    [Fact]
    public void Removing_a_barcode_returns_new_version_without_mutating_original()
    {
        var variant = new ProductVariant(
            ProductId.New(),
            "Standard",
            barcodes: [new ProductBarcode("123"), new ProductBarcode("456")]);

        var updated = variant.RemoveBarcode(new ProductBarcode(" 123 "));

        Assert.Equal(variant.Id, updated.Id);
        Assert.Equal(2, variant.Barcodes.Count);
        Assert.Single(updated.Barcodes);
        Assert.Equal("456", updated.Barcodes.Single().Code);
    }

    [Fact]
    public void Revising_details_preserves_identity_and_does_not_mutate_original()
    {
        var variant = new ProductVariant(
            ProductId.New(),
            "Standard",
            "SKU-1",
            [new ProductBarcode("123")]);

        var revised = variant.Revise(
            "Premium",
            "SKU-2",
            [new ProductBarcode("456")]);

        Assert.Equal(variant.Id, revised.Id);
        Assert.Equal("Standard", variant.Name);
        Assert.Equal("SKU-1", variant.Sku);
        Assert.Equal("123", variant.Barcodes.Single().Code);
        Assert.Equal("Premium", revised.Name);
        Assert.Equal("SKU-2", revised.Sku);
        Assert.Equal("456", revised.Barcodes.Single().Code);
    }

    [Fact]
    public void Removing_missing_barcode_is_rejected()
    {
        var variant = new ProductVariant(ProductId.New(), "Standard");

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => variant.RemoveBarcode(new ProductBarcode("missing")));

        Assert.Equal(DomainErrorCodes.BarcodeNotFound, exception.Code);
    }

    [Fact]
    public void Duplicate_barcodes_are_rejected_during_creation()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(
            () => new ProductVariant(
                ProductId.New(),
                "Standard",
                barcodes: [new ProductBarcode("123"), new ProductBarcode("123")]));

        Assert.Equal(DomainErrorCodes.DuplicateBarcode, exception.Code);
    }

    [Fact]
    public void Null_barcode_item_is_rejected_during_creation()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new ProductVariant(
                ProductId.New(),
                "Standard",
                barcodes: new[] { (ProductBarcode)null! }));

        Assert.Equal(DomainErrorCodes.NullCollectionItem, exception.Code);
    }

    [Fact]
    public void Null_barcode_collection_is_rejected_by_revise()
    {
        var variant = new ProductVariant(ProductId.New(), "Standard");

        var exception = Assert.Throws<DomainValidationException>(
            () => variant.Revise("Standard", null, null!));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
        Assert.Equal("barcodes", exception.PropertyName);
    }
}
