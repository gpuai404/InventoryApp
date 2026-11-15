using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;

namespace WarrantyIssueXE.Application.Abstractions.Persistence;

public interface IWarrantyPlanReader
{
    Task<WarrantyPlan?> GetByIdAsync(
        WarrantyPlanId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarrantyPlan>> ListAsync(CancellationToken cancellationToken = default);
}
