using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.Common;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Application.WarrantyPlans;

public sealed class WarrantyPlanCommandService
{
    private readonly IWarrantyPlanReader _reader;
    private readonly IWarrantyPlanWriter _writer;
    private readonly IUnitOfWork _unitOfWork;

    public WarrantyPlanCommandService(
        IWarrantyPlanReader reader,
        IWarrantyPlanWriter writer,
        IUnitOfWork unitOfWork)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<WarrantyPlanDto> CreateAsync(
        CreateWarrantyPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = new WarrantyPlan(
            request.Type,
            request.Name,
            new WarrantyTerm(request.TermMonths),
            request.CoverageDescription);

        await _writer.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return plan.ToDto();
    }

    public async Task<WarrantyPlanDto> UpdateAsync(
        Guid warrantyPlanId,
        UpdateWarrantyPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var typedId = new WarrantyPlanId(warrantyPlanId);
        var plan = await _reader
            .GetByIdAsync(typedId, cancellationToken)
            .RequireFoundAsync(typedId.Value);
        var revised = plan.Revise(
            request.Name,
            new WarrantyTerm(request.TermMonths),
            request.CoverageDescription);

        await _writer.UpdateAsync(revised, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return revised.ToDto();
    }

    public async Task ArchiveAsync(Guid warrantyPlanId, CancellationToken cancellationToken = default)
    {
        var typedId = new WarrantyPlanId(warrantyPlanId);
        await _reader
            .GetByIdAsync(typedId, cancellationToken)
            .RequireFoundAsync(typedId.Value);
        await _writer.ArchiveAsync(typedId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
