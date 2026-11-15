using WarrantyIssueXE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WarrantyIssueXE.Infrastructure;

public static class InventoryDatabaseMigration
{
    /// <summary>
    /// Applies the known WarrantyIssueXE schema migrations to the configured local
    /// database. The application host should call this during startup before it
    /// serves UI operations.
    /// </summary>
    public static async Task MigrateInventoryDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<InventoryDbContext>>();
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
