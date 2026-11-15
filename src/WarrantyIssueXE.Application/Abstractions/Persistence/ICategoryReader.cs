using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;

namespace WarrantyIssueXE.Application.Abstractions.Persistence;

public interface ICategoryReader
{
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);
}
