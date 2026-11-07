using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Application.Common;
using InventoryApp.Application.Products;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Exceptions;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Application.Tests;

public sealed class ProductApplicationServiceTests
{
    [Fact]
    public async Task CreateProductAsync_builds_and_persists_a_domain_product()
    {
        var category = new Category("Cables");
        var products = new FakeProductRepository();
        var variants = new FakeProductVariantRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new ProductCommandService(
            new FakeCategoryReader(category),
            products,
            products,
            variants,
            variants,
            unitOfWork);

        var result = await service.CreateProductAsync(
            new CreateProductRequest(
                category.Id.Value,
                " USB-C Cable ",
                12.50m,
                "eur",
                Description: "Fast charging"));

        Assert.Equal("USB-C Cable", result.Name);
        Assert.Equal(12.50m, result.PriceAmount);
        Assert.Equal("EUR", result.CurrencyCode);
        Assert.Equal("Fast charging", result.Description);
        Assert.Equal(category.Id.Value, result.CategoryId);
        Assert.Single(products.Items);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateProductVariantAsync_requires_the_product_and_commits_once()
    {
        var products = new FakeProductRepository();
        var product = new Product(CategoryId.New(), "Cable", new(10m, "EUR"));
        products.Items[product.Id] = product;

        var variants = new FakeProductVariantRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new ProductCommandService(
            new FakeCategoryReader(),
            products,
            products,
            variants,
            variants,
            unitOfWork);

        var result = await service.CreateProductVariantAsync(
            new CreateProductVariantRequest(
                product.Id.Value,
                "1 metre",
                "CAB-1M",
                [new ProductBarcodeInput(" 123 ", "Retail box")]));

        Assert.Equal(product.Id.Value, result.ProductId);
        Assert.Equal("123", result.Barcodes.Single().Code);
        Assert.Single(variants.Items);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateProductAsync_reassigns_the_product_to_an_existing_category()
    {
        var originalCategory = new Category("Cables");
        var replacementCategory = new Category("Accessories");
        var product = new Product(originalCategory.Id, "USB-C Cable", new(12.50m, "EUR"));
        var products = new FakeProductRepository();
        products.Items[product.Id] = product;
        var variants = new FakeProductVariantRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new ProductCommandService(
            new FakeCategoryReader(originalCategory, replacementCategory),
            products,
            products,
            variants,
            variants,
            unitOfWork);

        var result = await service.UpdateProductAsync(
            product.Id.Value,
            new UpdateProductRequest(
                replacementCategory.Id.Value,
                "USB-C Cable",
                12.50m,
                "EUR"));

        Assert.Equal(product.Id.Value, result.Id);
        Assert.Equal(replacementCategory.Id.Value, result.CategoryId);
        Assert.Equal(originalCategory.Id, product.CategoryId);
        Assert.Equal(replacementCategory.Id, products.Items[product.Id].CategoryId);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateProductVariantAsync_throws_when_product_does_not_exist()
    {
        var products = new FakeProductRepository();
        var variants = new FakeProductVariantRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new ProductCommandService(
            new FakeCategoryReader(),
            products,
            products,
            variants,
            variants,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.CreateProductVariantAsync(
                new CreateProductVariantRequest(
                    Guid.NewGuid(),
                    "1 metre")));

        Assert.Equal(nameof(Product), exception.EntityName);
        Assert.Empty(variants.Items);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddBarcodeAsync_uses_the_domain_transition_and_saves_the_new_version()
    {
        var product = new Product(CategoryId.New(), "Cable", new(10m, "EUR"));
        var original = new ProductVariant(product.Id, "1 metre");
        var products = new FakeProductRepository();
        var variants = new FakeProductVariantRepository();
        variants.Items[original.Id] = original;
        var unitOfWork = new FakeUnitOfWork();
        var service = new ProductCommandService(
            new FakeCategoryReader(),
            products,
            products,
            variants,
            variants,
            unitOfWork);

        var result = await service.AddBarcodeAsync(
            original.Id.Value,
            new AddProductVariantBarcodeRequest("456", "Retail box"));

        Assert.Empty(original.Barcodes);
        Assert.Equal("456", result.Barcodes.Single().Code);
        Assert.NotSame(original, variants.Items[original.Id]);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddBarcodeAsync_preserves_domain_rule_failures()
    {
        var product = new Product(CategoryId.New(), "Cable", new(10m, "EUR"));
        var original = new ProductVariant(
            product.Id,
            "1 metre",
            barcodes: [new("456")]);
        var products = new FakeProductRepository();
        var variants = new FakeProductVariantRepository();
        variants.Items[original.Id] = original;
        var unitOfWork = new FakeUnitOfWork();
        var service = new ProductCommandService(
            new FakeCategoryReader(),
            products,
            products,
            variants,
            variants,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            service.AddBarcodeAsync(
                original.Id.Value,
                new AddProductVariantBarcodeRequest("456")));

        Assert.Equal(DomainErrorCodes.DuplicateBarcode, exception.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RemoveBarcodeAsync_uses_the_domain_transition_and_saves_the_new_version()
    {
        var product = new Product(CategoryId.New(), "Cable", new(10m, "EUR"));
        var original = new ProductVariant(
            product.Id,
            "1 metre",
            barcodes: [new("456"), new("789")]);
        var products = new FakeProductRepository();
        var variants = new FakeProductVariantRepository();
        variants.Items[original.Id] = original;
        var unitOfWork = new FakeUnitOfWork();
        var service = new ProductCommandService(
            new FakeCategoryReader(),
            products,
            products,
            variants,
            variants,
            unitOfWork);

        var result = await service.RemoveBarcodeAsync(
            original.Id.Value,
            new RemoveProductVariantBarcodeRequest("456"));

        Assert.Equal(["789"], result.Barcodes.Select(barcode => barcode.Code));
        Assert.Equal(2, original.Barcodes.Count);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Query_service_has_no_write_dependency_and_maps_read_models()
    {
        var product = new Product(CategoryId.New(), "Cable", new(10m, "EUR"));
        var variant = new ProductVariant(
            product.Id,
            "1 metre",
            barcodes: [new("456")]);
        var products = new FakeProductRepository();
        products.Items[product.Id] = product;
        var variants = new FakeProductVariantRepository();
        variants.Items[variant.Id] = variant;
        var service = new ProductQueryService(products, variants);

        var result = await service.FindVariantByBarcodeAsync(" 456 ");

        Assert.NotNull(result);
        Assert.Equal(variant.Id.Value, result.Id);
        Assert.Equal("456", result.Barcodes.Single().Code);
    }

    [Fact]
    public async Task CreateProductVariantAsync_rejects_a_barcode_assigned_to_another_variant()
    {
        var product = new Product(CategoryId.New(), "Cable", new(10m, "EUR"));
        var existingVariant = new ProductVariant(product.Id, "1 metre", barcodes: [new("456")]);
        var products = new FakeProductRepository();
        products.Items[product.Id] = product;
        var variants = new FakeProductVariantRepository();
        variants.Items[existingVariant.Id] = existingVariant;
        var unitOfWork = new FakeUnitOfWork();
        var service = new ProductCommandService(
            new FakeCategoryReader(),
            products,
            products,
            variants,
            variants,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            service.CreateProductVariantAsync(new CreateProductVariantRequest(
                product.Id.Value,
                "2 metres",
                Barcodes: [new ProductBarcodeInput("456")])));

        Assert.Equal(DomainErrorCodes.DuplicateBarcode, exception.Code);
        Assert.Single(variants.Items);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private sealed class FakeCategoryReader(params Category[] categories) : ICategoryReader
    {
        private readonly IReadOnlyDictionary<CategoryId, Category> _items =
            categories.ToDictionary(category => category.Id);

        public Task<Category?> GetByIdAsync(
            CategoryId categoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.GetValueOrDefault(categoryId));

        public Task<IReadOnlyList<Category>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>(_items.Values.ToArray());
    }

    private sealed class FakeProductRepository : IProductReader, IProductWriter
    {
        public Dictionary<ProductId, Product> Items { get; } = [];

        public Task<Product?> GetByIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(productId));

        public Task<IReadOnlyList<Product>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Product>>(Items.Values.ToArray());

        public Task AddAsync(
            Product product,
            CancellationToken cancellationToken = default)
        {
            Items[product.Id] = product;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Product product,
            CancellationToken cancellationToken = default)
        {
            Items[product.Id] = product;
            return Task.CompletedTask;
        }

        public Task ArchiveAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
        {
            Items.Remove(productId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProductVariantRepository : IProductVariantReader, IProductVariantWriter
    {
        public Dictionary<ProductVariantId, ProductVariant> Items { get; } = [];

        public Task<ProductVariant?> GetByIdAsync(
            ProductVariantId variantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(variantId));

        public Task<IReadOnlyList<ProductVariant>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductVariant>>(Items.Values.ToArray());

        public Task<ProductVariant?> FindByBarcodeAsync(
            ProductBarcode barcode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Values.SingleOrDefault(variant =>
                variant.Barcodes.Any(candidate =>
                    string.Equals(candidate.Code, barcode.Code, StringComparison.Ordinal))));

        public Task<bool> IsBarcodeReservedAsync(
            ProductBarcode barcode,
            ProductVariantId? excludingVariantId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Values.Any(variant =>
                variant.Id != excludingVariantId &&
                variant.Barcodes.Any(candidate =>
                    string.Equals(candidate.Code, barcode.Code, StringComparison.Ordinal))));

        public Task AddAsync(
            ProductVariant variant,
            CancellationToken cancellationToken = default)
        {
            Items[variant.Id] = variant;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            ProductVariant variant,
            CancellationToken cancellationToken = default)
        {
            Items[variant.Id] = variant;
            return Task.CompletedTask;
        }

        public Task ArchiveAsync(
            ProductVariantId variantId,
            CancellationToken cancellationToken = default)
        {
            Items.Remove(variantId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
