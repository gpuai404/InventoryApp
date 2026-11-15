using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.Common;
using WarrantyIssueXE.Application.Products;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Application.Tests;

public sealed class ProductApplicationServiceTests
{
    [Fact]
    public async Task Create_product_requires_category_and_persists_domain_product()
    {
        var category = new Category("Cables");
        var products = new FakeProductRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(new FakeCategoryReader(category), products, unitOfWork);

        var result = await service.CreateProductAsync(new CreateProductRequest(
            category.Id.Value,
            " USB-C Cable ",
            12.50m,
            "eur",
            "Fast charging"));

        Assert.Equal("USB-C Cable", result.Name);
        Assert.Equal(category.Id.Value, result.CategoryId);
        Assert.Equal(12.50m, result.PriceAmount);
        Assert.Equal("EUR", result.CurrencyCode);
        Assert.Equal("Fast charging", result.Description);
        Assert.Single(products.Items);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_product_rejects_missing_category_without_persisting()
    {
        var products = new FakeProductRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(new FakeCategoryReader(), products, unitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.CreateProductAsync(
            new CreateProductRequest(Guid.NewGuid(), "Cable", 10m, "EUR")));

        Assert.Empty(products.Items);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_product_preserves_identity_and_requires_existing_category()
    {
        var originalCategory = new Category("Cables");
        var replacementCategory = new Category("Accessories");
        var original = new Product(originalCategory.Id, "Cable", new Money(10m, "EUR"));
        var products = new FakeProductRepository();
        products.Items[original.Id] = original;
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            new FakeCategoryReader(originalCategory, replacementCategory),
            products,
            unitOfWork);

        var result = await service.UpdateProductAsync(
            original.Id.Value,
            new UpdateProductRequest(
                replacementCategory.Id.Value,
                "Cable Pro",
                15m,
                "EUR"));

        Assert.Equal(original.Id.Value, result.Id);
        Assert.Equal(replacementCategory.Id.Value, result.CategoryId);
        Assert.Equal("Cable Pro", products.Items[original.Id].Name);
        Assert.Equal(originalCategory.Id, original.CategoryId);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_product_rejects_missing_product_without_persisting()
    {
        var category = new Category("Cables");
        var products = new FakeProductRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(new FakeCategoryReader(category), products, unitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.UpdateProductAsync(
            Guid.NewGuid(),
            new UpdateProductRequest(category.Id.Value, "Cable", 10m, "EUR")));

        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Archive_product_requires_existing_product_and_commits_once()
    {
        var product = new Product(CategoryId.New(), "Cable", new Money(10m, "EUR"));
        var products = new FakeProductRepository();
        products.Items[product.Id] = product;
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(new FakeCategoryReader(), products, unitOfWork);

        await service.ArchiveProductAsync(product.Id.Value);

        Assert.Empty(products.Items);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Query_service_returns_product_read_models()
    {
        var first = new Product(CategoryId.New(), "Cable", new Money(10m, "EUR"));
        var second = new Product(CategoryId.New(), "Adapter", new Money(20m, "EUR"));
        var products = new FakeProductRepository();
        products.Items[first.Id] = first;
        products.Items[second.Id] = second;
        var service = new ProductQueryService(products);

        var selected = await service.GetProductAsync(first.Id.Value);
        var all = await service.ListProductsAsync();

        Assert.Equal(first.Id.Value, selected!.Id);
        Assert.Equal(2, all.Count);
    }

    private static ProductCommandService CreateService(
        ICategoryReader categories,
        FakeProductRepository products,
        IUnitOfWork unitOfWork) =>
        new(categories, products, products, unitOfWork);

    private sealed class FakeCategoryReader(params Category[] categories) : ICategoryReader
    {
        private readonly IReadOnlyDictionary<CategoryId, Category> _items =
            categories.ToDictionary(category => category.Id);

        public Task<Category?> GetByIdAsync(
            CategoryId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>(_items.Values.ToArray());
    }

    private sealed class FakeProductRepository : IProductReader, IProductWriter
    {
        public Dictionary<ProductId, Product> Items { get; } = [];

        public Task<Product?> GetByIdAsync(
            ProductId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(id));

        public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Product>>(Items.Values.ToArray());

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            Items[product.Id] = product;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            Items[product.Id] = product;
            return Task.CompletedTask;
        }

        public Task ArchiveAsync(ProductId id, CancellationToken cancellationToken = default)
        {
            Items.Remove(id);
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
