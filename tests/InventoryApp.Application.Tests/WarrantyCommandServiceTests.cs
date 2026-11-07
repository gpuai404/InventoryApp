using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Application.Warranties;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Application.Tests;

public sealed class WarrantyCommandServiceTests
{
    [Fact]
    public async Task CreateAsync_derives_the_expiration_from_the_selected_service_term()
    {
        var company = new Company("Acme Repairs", CompanyContact.Empty);
        var product = new Product(CategoryId.New(), "USB-C Cable", new Money(12.50m, "EUR"));
        var service = new Service(company.Id, "New sale", new WarrantyTerm(24));
        var products = new FakeProductReader(product);
        var services = new FakeServiceReader(service);
        var warranties = new FakeWarrantyRepository();
        var unitOfWork = new FakeUnitOfWork();
        var commandService = new WarrantyCommandService(
            products,
            services,
            warranties,
            warranties,
            unitOfWork);

        var result = await commandService.CreateAsync(
            new CreateWarrantyRequest(
                product.Id.Value,
                service.Id.Value,
                "Two-year new-sale warranty",
                new DateOnly(2026, 1, 1)));

        Assert.Equal(new DateOnly(2027, 12, 31), result.ExpiresOn);
        Assert.Equal(service.Id.Value, result.ServiceId);
        Assert.Equal("Two-year new-sale warranty", result.Description);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Single(warranties.Items);
    }

    [Fact]
    public async Task UpdateAsync_derives_the_expiration_from_the_reselected_service_term()
    {
        var company = new Company("Acme Repairs", CompanyContact.Empty);
        var product = new Product(CategoryId.New(), "USB-C Cable", new Money(12.50m, "EUR"));
        var originalService = new Service(company.Id, "New sale", new WarrantyTerm(24));
        var repairService = new Service(company.Id, "Repair", new WarrantyTerm(6));
        var warranty = Warranty.Issue(
            product.Id,
            originalService.Id,
            "Two-year new-sale warranty",
            new DateOnly(2026, 1, 1),
            originalService.WarrantyTerm);
        var warranties = new FakeWarrantyRepository();
        await warranties.AddAsync(warranty);
        var unitOfWork = new FakeUnitOfWork();
        var commandService = new WarrantyCommandService(
            new FakeProductReader(product),
            new FakeServiceReader(originalService, repairService),
            warranties,
            warranties,
            unitOfWork);

        var result = await commandService.UpdateAsync(
            warranty.Id.Value,
            new UpdateWarrantyRequest(
                product.Id.Value,
                repairService.Id.Value,
                "Six-month repair warranty",
                new DateOnly(2026, 2, 1)));

        Assert.Equal(repairService.Id.Value, result.ServiceId);
        Assert.Equal(new DateOnly(2026, 7, 31), result.ExpiresOn);
        Assert.Equal("Six-month repair warranty", result.Description);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    private sealed class FakeProductReader(params Product[] products) : IProductReader
    {
        private readonly IReadOnlyDictionary<ProductId, Product> _items =
            products.ToDictionary(product => product.Id);

        public Task<Product?> GetByIdAsync(
            ProductId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<Product>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Product>>(_items.Values.ToArray());
    }

    private sealed class FakeServiceReader(params Service[] services) : IServiceReader
    {
        private readonly IReadOnlyDictionary<ServiceId, Service> _items =
            services.ToDictionary(service => service.Id);

        public Task<Service?> GetByIdAsync(
            ServiceId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<Service>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Service>>(_items.Values.ToArray());
    }

    private sealed class FakeWarrantyRepository : IWarrantyReader, IWarrantyWriter
    {
        public Dictionary<WarrantyId, Warranty> Items { get; } = [];

        public Task<Warranty?> GetByIdAsync(
            WarrantyId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(id));

        public Task<IReadOnlyList<Warranty>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Warranty>>(Items.Values.ToArray());

        public Task AddAsync(Warranty entity, CancellationToken cancellationToken = default)
        {
            Items[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Warranty entity, CancellationToken cancellationToken = default)
        {
            Items[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task ArchiveAsync(WarrantyId id, CancellationToken cancellationToken = default)
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
