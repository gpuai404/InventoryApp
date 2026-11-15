using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Infrastructure.Persistence;
using WarrantyIssueXE.Infrastructure.Persistence.Mappings;
using Microsoft.EntityFrameworkCore;

namespace WarrantyIssueXE.Infrastructure.Persistence.Repositories;

public sealed class WarrantyPlanRepository(InventoryDbContext dbContext, TimeProvider timeProvider)
    : IWarrantyPlanReader, IWarrantyPlanWriter
{
    private readonly InventoryDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<WarrantyPlan?> GetByIdAsync(
        WarrantyPlanId id,
        CancellationToken cancellationToken = default) =>
        (await _dbContext.WarrantyPlans.AsNoTracking().SingleOrDefaultAsync(
            plan => plan.Id == id.Value,
            cancellationToken))?.ToDomain();

    public async Task<IReadOnlyList<WarrantyPlan>> ListAsync(CancellationToken cancellationToken = default) =>
        (await _dbContext.WarrantyPlans.AsNoTracking()
            .OrderBy(plan => plan.Name)
            .ToListAsync(cancellationToken))
        .Select(plan => plan.ToDomain())
        .ToArray();

    public Task AddAsync(WarrantyPlan entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbContext.WarrantyPlans.Add(entity.ToRecord());
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(WarrantyPlan entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var record = await _dbContext.WarrantyPlans.SingleOrDefaultAsync(
            plan => plan.Id == entity.Id.Value,
            cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(WarrantyPlan), entity.Id.Value);

        record.Type = entity.Type.ToString();
        record.Name = entity.Name;
        record.TermMonths = entity.Term.Months;
        record.CoverageDescription = entity.CoverageDescription;
    }

    public async Task ArchiveAsync(WarrantyPlanId id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.WarrantyPlans.SingleOrDefaultAsync(
            plan => plan.Id == id.Value,
            cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(WarrantyPlan), id.Value);

        record.ArchivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
    }
}
