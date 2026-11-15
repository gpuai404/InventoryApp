using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;

namespace WarrantyIssueXE.Application.Abstractions.Persistence;

public interface IWarrantyReader
{
    Task<Warranty?> GetByIdAsync(WarrantyId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Warranty>> ListAsync(CancellationToken cancellationToken = default);

    Task<Warranty?> GetByCertificateNumberAsync(
        string certificateNumber,
        CancellationToken cancellationToken = default);
}
