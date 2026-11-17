using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.Categories;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Application.Tests;

public sealed class CategoryCommandServiceTests
{
    [Fact]
    public async Task Archive_unused_category_archives_and_commits_once()
    {
        var category = new Category("Unused");
        var categories = new FakeCategoryRepository(category);
        var products = new FakeProductReader();
        var unitOfWork = new FakeUnitOfWork();
        var service = new CategoryCommandService(categories, categories, products, unitOfWork);

        await service.ArchiveAsync(category.Id.Value);

        Assert.Null(await categories.GetByIdAsync(category.Id));
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Archive_category_with_active_product_is_rejected_without_changes()
    {
        var category = new Category("In use");
        var product = new Product(category.Id, "Phone", new Money(450m, "EUR"));
        var categories = new FakeCategoryRepository(category);
        var products = new FakeProductReader(product);
        var unitOfWork = new FakeUnitOfWork();
        var service = new CategoryCommandService(categories, categories, products, unitOfWork);

        var exception = await Assert.ThrowsAsync<CategoryInUseException>(
            () => service.ArchiveAsync(category.Id.Value));

        Assert.Equal(category.Id.Value, exception.CategoryId);
        Assert.Contains("Reassign or archive", exception.Message, StringComparison.Ordinal);
        Assert.NotNull(await categories.GetByIdAsync(category.Id));
        Assert.Equal(0, categories.ArchiveCount);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private sealed class FakeCategoryRepository(params Category[] categories)
        : ICategoryReader, ICategoryWriter
    {
        private readonly Dictionary<CategoryId, Category> _items =
            categories.ToDictionary(category => category.Id);

        public int ArchiveCount { get; private set; }

        public Task<Category?> GetByIdAsync(
            CategoryId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>(_items.Values.ToArray());

        public Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            _items[category.Id] = category;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
        {
            _items[category.Id] = category;
            return Task.CompletedTask;
        }

        public Task ArchiveAsync(CategoryId id, CancellationToken cancellationToken = default)
        {
            ArchiveCount++;
            _items.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProductReader(params Product[] products) : IProductReader
    {
        private readonly IReadOnlyDictionary<ProductId, Product> _items =
            products.ToDictionary(product => product.Id);

        public Task<Product?> GetByIdAsync(
            ProductId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Product>>(_items.Values.ToArray());

        public Task<bool> HasActiveProductsInCategoryAsync(
            CategoryId categoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Values.Any(product => product.CategoryId == categoryId));
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
