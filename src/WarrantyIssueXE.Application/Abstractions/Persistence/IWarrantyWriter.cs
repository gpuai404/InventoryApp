using WarrantyIssueXE.Domain.Entities;

namespace WarrantyIssueXE.Application.Abstractions.Persistence;

public interface IWarrantyWriter
{
    Task AddAsync(Warranty warranty, CancellationToken cancellationToken = default);
}
