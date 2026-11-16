using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.IssuerProfiles;
using Microsoft.EntityFrameworkCore;

namespace WarrantyIssueXE.Infrastructure.Persistence;

public sealed class EfUnitOfWork(InventoryDbContext dbContext) : IUnitOfWork
{
    private readonly InventoryDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            PersistenceErrors.IsConcurrentIssuerProfileCreation(exception))
        {
            throw new IssuerProfileConflictException();
        }
    }
}
