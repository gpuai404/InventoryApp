using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;

namespace WarrantyIssueXE.Application.Abstractions.Persistence;

public interface IWarrantyPlanWriter
{
    Task AddAsync(WarrantyPlan plan, CancellationToken cancellationToken = default);

    Task UpdateAsync(WarrantyPlan plan, CancellationToken cancellationToken = default);

    Task ArchiveAsync(WarrantyPlanId id, CancellationToken cancellationToken = default);
}
