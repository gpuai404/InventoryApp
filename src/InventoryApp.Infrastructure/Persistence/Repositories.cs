using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Persistence;

public sealed class CategoryRepository(
    InventoryDbContext dbContext,
    TimeProvider? timeProvider = null)
    : ICategoryReader, ICategoryWriter
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<Category?> GetByIdAsync(
        CategoryId id,
        CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.Id == id.Value, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Category>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

        return records.Select(category => category.ToDomain()).ToArray();
    }

    public Task AddAsync(Category entity, CancellationToken cancellationToken = default)
    {
        dbContext.Categories.Add(entity.ToRecord());
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Categories
            .SingleOrDefaultAsync(category => category.Id == entity.Id.Value, cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Category), entity.Id.Value);

        record.Name = entity.Name;
        record.Description = entity.Description;
    }

    public async Task ArchiveAsync(CategoryId id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Categories
            .SingleOrDefaultAsync(category => category.Id == id.Value, cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Category), id.Value);

        record.DeletedOnUtc = _timeProvider.GetUtcNow().UtcDateTime;
    }

}

public sealed class CompanyRepository(
    InventoryDbContext dbContext,
    TimeProvider? timeProvider = null)
    : ICompanyReader, ICompanyWriter
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<Company?> GetByIdAsync(
        CompanyId id,
        CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(company => company.Id == id.Value, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Company>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Companies
            .AsNoTracking()
            .OrderBy(company => company.Name)
            .ToListAsync(cancellationToken);

        return records.Select(company => company.ToDomain()).ToArray();
    }

    public Task AddAsync(Company entity, CancellationToken cancellationToken = default)
    {
        dbContext.Companies.Add(entity.ToRecord());
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Company entity, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Companies
            .SingleOrDefaultAsync(company => company.Id == entity.Id.Value, cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Company), entity.Id.Value);

        record.Name = entity.Name;
        record.Address = entity.Contact.Address;
        record.Email = entity.Contact.Email;
        record.Phone = entity.Contact.Phone;
    }

    public async Task ArchiveAsync(CompanyId id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Companies
            .SingleOrDefaultAsync(company => company.Id == id.Value, cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Company), id.Value);

        record.DeletedOnUtc = _timeProvider.GetUtcNow().UtcDateTime;
    }

}

public sealed class ProductRepository(
    InventoryDbContext dbContext,
    TimeProvider? timeProvider = null)
    : IProductReader, IProductWriter
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<Product?> GetByIdAsync(
        ProductId id,
        CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == id.Value, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Product>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

        return records.Select(product => product.ToDomain()).ToArray();
    }

    public Task AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        dbContext.Products.Add(entity.ToRecord());
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Products
            .SingleOrDefaultAsync(product => product.Id == entity.Id.Value, cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Product), entity.Id.Value);

        record.CategoryId = entity.CategoryId.Value;
        record.Name = entity.Name;
        record.PriceAmount = entity.Price.Amount;
        record.PriceCurrencyCode = entity.Price.CurrencyCode;
        record.Description = entity.Description;
    }

    public async Task ArchiveAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Products
            .SingleOrDefaultAsync(product => product.Id == id.Value, cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Product), id.Value);

        record.DeletedOnUtc = _timeProvider.GetUtcNow().UtcDateTime;
    }

}

public sealed class ProductVariantRepository(
    InventoryDbContext dbContext,
    TimeProvider? timeProvider = null)
    : IProductVariantReader, IProductVariantWriter
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<ProductVariant?> GetByIdAsync(
        ProductVariantId id,
        CancellationToken cancellationToken = default)
    {
        var record = await QueryWithBarcodes()
            .AsNoTracking()
            .SingleOrDefaultAsync(variant => variant.Id == id.Value, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<ProductVariant>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await QueryWithBarcodes()
            .AsNoTracking()
            .OrderBy(variant => variant.Name)
            .ToListAsync(cancellationToken);

        return records.Select(variant => variant.ToDomain()).ToArray();
    }

    public async Task<ProductVariant?> FindByBarcodeAsync(
        ProductBarcode barcode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(barcode);

        var record = await QueryWithBarcodes()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                variant => variant.Barcodes.Any(candidate => candidate.Code == barcode.Code),
                cancellationToken);

        return record?.ToDomain();
    }

    public Task<bool> IsBarcodeReservedAsync(
        ProductBarcode barcode,
        ProductVariantId? excludingVariantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(barcode);

        var query = dbContext.ProductVariants
            .IgnoreQueryFilters()
            .Where(variant => variant.Barcodes.Any(candidate => candidate.Code == barcode.Code));

        if (excludingVariantId is not null)
        {
            query = query.Where(variant => variant.Id != excludingVariantId.Value.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(ProductVariant entity, CancellationToken cancellationToken = default)
    {
        dbContext.ProductVariants.Add(entity.ToRecord());
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(ProductVariant entity, CancellationToken cancellationToken = default)
    {
        var record = await QueryWithBarcodes()
            .SingleOrDefaultAsync(variant => variant.Id == entity.Id.Value, cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(ProductVariant), entity.Id.Value);

        record.ProductId = entity.ProductId.Value;
        record.Name = entity.Name;
        record.Sku = entity.Sku;
        record.Barcodes.Clear();
        record.Barcodes.AddRange(entity.Barcodes.Select(barcode => barcode.ToRecord(entity.Id)));
    }

    public async Task ArchiveAsync(ProductVariantId id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.ProductVariants
            .SingleOrDefaultAsync(variant => variant.Id == id.Value, cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(ProductVariant), id.Value);

        record.DeletedOnUtc = _timeProvider.GetUtcNow().UtcDateTime;
    }

    private IQueryable<ProductVariantRecord> QueryWithBarcodes() =>
        dbContext.ProductVariants.Include(variant => variant.Barcodes);

}

public sealed class ServiceRepository(
    InventoryDbContext dbContext,
    TimeProvider? timeProvider = null)
    : IServiceReader, IServiceWriter
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<Service?> GetByIdAsync(
        ServiceId id,
        CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Services
            .AsNoTracking()
            .SingleOrDefaultAsync(service => service.Id == id.Value, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Service>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Services
            .AsNoTracking()
            .OrderBy(service => service.Name)
            .ToListAsync(cancellationToken);

        return records.Select(service => service.ToDomain()).ToArray();
    }

    public Task AddAsync(Service entity, CancellationToken cancellationToken = default)
    {
        dbContext.Services.Add(entity.ToRecord());
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Service entity, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Services
            .SingleOrDefaultAsync(service => service.Id == entity.Id.Value, cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Service), entity.Id.Value);

        record.CompanyId = entity.CompanyId.Value;
        record.Name = entity.Name;
        record.WarrantyTermMonths = entity.WarrantyTerm.Months;
        record.Description = entity.Description;
    }

    public async Task ArchiveAsync(ServiceId id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Services
            .SingleOrDefaultAsync(service => service.Id == id.Value, cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Service), id.Value);

        record.DeletedOnUtc = _timeProvider.GetUtcNow().UtcDateTime;
    }

}

public sealed class WarrantyRepository(
    InventoryDbContext dbContext,
    TimeProvider? timeProvider = null)
    : IWarrantyReader, IWarrantyWriter
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<Warranty?> GetByIdAsync(
        WarrantyId id,
        CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Warranties
            .AsNoTracking()
            .SingleOrDefaultAsync(warranty => warranty.Id == id.Value, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<IReadOnlyList<Warranty>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Warranties
            .AsNoTracking()
            .OrderBy(warranty => warranty.ExpiresOn)
            .ToListAsync(cancellationToken);

        return records.Select(warranty => warranty.ToDomain()).ToArray();
    }

    public Task AddAsync(Warranty entity, CancellationToken cancellationToken = default)
    {
        dbContext.Warranties.Add(entity.ToRecord());
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Warranty entity, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Warranties
            .SingleOrDefaultAsync(warranty => warranty.Id == entity.Id.Value, cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Warranty), entity.Id.Value);

        var replacement = entity.ToRecord();
        record.ProductId = replacement.ProductId;
        record.ServiceId = replacement.ServiceId;
        record.Description = replacement.Description;
        record.StartsOn = replacement.StartsOn;
        record.ExpiresOn = replacement.ExpiresOn;
    }

    public async Task ArchiveAsync(WarrantyId id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Warranties
            .SingleOrDefaultAsync(warranty => warranty.Id == id.Value, cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Warranty), id.Value);

        record.DeletedOnUtc = _timeProvider.GetUtcNow().UtcDateTime;
    }
}
