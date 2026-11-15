using WarrantyIssueXE.Domain.Entities;

namespace WarrantyIssueXE.Application.Abstractions.Persistence;

public interface IWarrantyWriter
{
    Task AddAsync(Warranty warranty, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages the sole permitted lifecycle transition. Implementations must not
    /// rewrite issuance facts or expose an archive/delete operation.
    /// </summary>
    Task VoidAsync(Warranty voidedWarranty, CancellationToken cancellationToken = default);
}
