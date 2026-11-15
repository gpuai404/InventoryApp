using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WarrantyIssueXE.Infrastructure.Persistence;

/// <summary>
/// Creates the context for EF Core tooling without depending on an application
/// host or targeting an application's SQLite database.
/// </summary>
public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "WarrantyIssueXE", "design-time.db");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new InventoryDbContext(options);
    }
}
