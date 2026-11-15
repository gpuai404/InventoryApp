using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Infrastructure.Persistence;
using WarrantyIssueXE.Infrastructure.Persistence.Mappings;
using Microsoft.EntityFrameworkCore;

namespace WarrantyIssueXE.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(InventoryDbContext dbContext, TimeProvider timeProvider)
    : IProductReader, IProductWriter
{
    private readonly InventoryDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default) =>
        (await _dbContext.Products.AsNoTracking().SingleOrDefaultAsync(
            product => product.Id == id.Value,
            cancellationToken))?.ToDomain();

    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) =>
        (await _dbContext.Products.AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken))
        .Select(product => product.ToDomain())
        .ToArray();

    public Task AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbContext.Products.Add(entity.ToRecord());
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var record = await _dbContext.Products.SingleOrDefaultAsync(
            product => product.Id == entity.Id.Value,
            cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Product), entity.Id.Value);

        record.CategoryId = entity.CategoryId.Value;
        record.Name = entity.Name;
        record.PriceAmount = entity.Price.Amount;
        record.PriceCurrencyCode = entity.Price.CurrencyCode;
        record.Description = entity.Description;
    }

    public async Task ArchiveAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Products.SingleOrDefaultAsync(
            product => product.Id == id.Value,
            cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Product), id.Value);

        record.ArchivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
    }
}
