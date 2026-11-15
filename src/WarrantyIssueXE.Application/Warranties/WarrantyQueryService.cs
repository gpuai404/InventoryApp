using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Domain.Identifiers;

namespace WarrantyIssueXE.Application.Warranties;

public sealed class WarrantyQueryService(IWarrantyReader reader)
{
    private readonly IWarrantyReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public async Task<WarrantyDto?> GetAsync(
        Guid warrantyId,
        CancellationToken cancellationToken = default) =>
        (await _reader.GetByIdAsync(new WarrantyId(warrantyId), cancellationToken))?.ToDto();

    public async Task<WarrantyDto?> GetByCertificateNumberAsync(
        string certificateNumber,
        CancellationToken cancellationToken = default) =>
        (await _reader.GetByCertificateNumberAsync(certificateNumber, cancellationToken))?.ToDto();

    public async Task<IReadOnlyList<WarrantyDto>> ListAsync(
        CancellationToken cancellationToken = default) =>
        (await _reader.ListAsync(cancellationToken))
        .Select(warranty => warranty.ToDto())
        .ToArray();
}
