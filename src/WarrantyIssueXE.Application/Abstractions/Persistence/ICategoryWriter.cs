using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;

namespace WarrantyIssueXE.Application.Abstractions.Persistence;

public interface ICategoryWriter
{
    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);

    Task ArchiveAsync(CategoryId id, CancellationToken cancellationToken = default);
}
