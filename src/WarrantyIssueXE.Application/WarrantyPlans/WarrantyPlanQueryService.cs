using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Domain.Identifiers;

namespace WarrantyIssueXE.Application.WarrantyPlans;

public sealed class WarrantyPlanQueryService(IWarrantyPlanReader reader)
{
    private readonly IWarrantyPlanReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public async Task<WarrantyPlanDto?> GetAsync(
        Guid warrantyPlanId,
        CancellationToken cancellationToken = default) =>
        (await _reader.GetByIdAsync(new WarrantyPlanId(warrantyPlanId), cancellationToken))?.ToDto();

    public async Task<IReadOnlyList<WarrantyPlanDto>> ListAsync(
        CancellationToken cancellationToken = default) =>
        (await _reader.ListAsync(cancellationToken)).Select(plan => plan.ToDto()).ToArray();
}
