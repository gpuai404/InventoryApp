using InventoryApp.Application.Abstractions.Persistence;

namespace InventoryApp.Infrastructure.Persistence;

public sealed class EfUnitOfWork(InventoryDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
