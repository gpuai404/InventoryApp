using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Infrastructure.Persistence;
using WarrantyIssueXE.Infrastructure.Persistence.Mappings;
using Microsoft.EntityFrameworkCore;

namespace WarrantyIssueXE.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(InventoryDbContext dbContext, TimeProvider timeProvider)
    : ICategoryReader, ICategoryWriter
{
    private readonly InventoryDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default) =>
        (await _dbContext.Categories.AsNoTracking().SingleOrDefaultAsync(
            category => category.Id == id.Value,
            cancellationToken))?.ToDomain();

    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
        (await _dbContext.Categories.AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken))
        .Select(category => category.ToDomain())
        .ToArray();

    public Task AddAsync(Category entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbContext.Categories.Add(entity.ToRecord());
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var record = await _dbContext.Categories.SingleOrDefaultAsync(
            category => category.Id == entity.Id.Value,
            cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Category), entity.Id.Value);

        record.Name = entity.Name;
        record.Description = entity.Description;
    }

    public async Task ArchiveAsync(CategoryId id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Categories.SingleOrDefaultAsync(
            category => category.Id == id.Value,
            cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(Category), id.Value);

        record.ArchivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
    }
}
