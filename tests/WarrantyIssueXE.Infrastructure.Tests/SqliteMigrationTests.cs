using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Domain.WarrantyDetails;
using WarrantyIssueXE.Infrastructure.Persistence;
using WarrantyIssueXE.Infrastructure.Persistence.Repositories;

namespace WarrantyIssueXE.Infrastructure.Tests;

public sealed class SqliteMigrationTests
{
    [Fact]
    public async Task Initial_migration_creates_and_reopens_a_complete_working_database()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"WarrantyIssueXE-migration-{Guid.NewGuid():N}.db");

        try
        {
            var category = new Category("Phones at issuance", "Original category description");
            var product = new Product(
                category.Id,
                "Migration Phone at issuance",
                new Money(1234.5678m, "eur"),
                "Created through the migrated schema");
            var issuer = new IssuerProfile(
                "Original Warranty Workshop",
                "BE0123456789",
                "Original Warranty Street 10",
                "original@example.test",
                "+321111111");
            var productSnapshot = product.ToSnapshot(category.Name);
            var issuerSnapshot = issuer.ToSnapshot();
            var saleIssuedAt = new DateTimeOffset(2026, 10, 25, 2, 30, 45, TimeSpan.FromHours(2));
            var salePurchasedOn = new DateOnly(2026, 10, 24);
            var saleCustomer = new CustomerSnapshot(
                "Ada Lovelace",
                "ada@example.test",
                "+322222222",
                "Customer Lane 5");
            var saleWarranty = Warranty.IssueForSale(
                "migration-sale-001",
                WarrantyPlanId.New(),
                new Imei("490154203237518"),
                saleIssuedAt,
                new DateOnly(2026, 10, 25),
                saleCustomer,
                productSnapshot,
                issuerSnapshot,
                CoverageSnapshot.ForProductSale(new WarrantyTerm(24), "Manufacturing defects"),
                new SaleDetails(salePurchasedOn));
            var repairWarranty = Warranty.IssueForRepair(
                "migration-repair-001",
                WarrantyPlanId.New(),
                imei: null,
                new DateTimeOffset(2026, 11, 2, 16, 15, 0, TimeSpan.FromHours(1)),
                new DateOnly(2026, 11, 2),
                new CustomerSnapshot("Grace Hopper", "grace@example.test"),
                productSnapshot,
                issuerSnapshot,
                CoverageSnapshot.ForRepair(new WarrantyTerm(6), "Repair workmanship"),
                new RepairDetails(
                    new DateOnly(2026, 11, 2),
                    "Replaced the display and battery",
                    [
                        new ReplacedPartSnapshot("Display module", "DSP-100"),
                        new ReplacedPartSnapshot("Battery", "BAT-200")
                    ]));
            var laborOnlyWarranty = Warranty.IssueForRepair(
                "migration-repair-labor-001",
                WarrantyPlanId.New(),
                imei: null,
                new DateTimeOffset(2026, 11, 3, 9, 0, 0, TimeSpan.FromHours(1)),
                new DateOnly(2026, 11, 3),
                new CustomerSnapshot("Katherine Johnson", phone: "+323333333"),
                productSnapshot,
                issuerSnapshot,
                CoverageSnapshot.ForRepair(new WarrantyTerm(3), "Labor workmanship"),
                new RepairDetails(new DateOnly(2026, 11, 3), "Labor-only board repair"));

            DbConnection? firstConnection = null;
            await using (var services = CreateServices(databasePath))
            {
                await using (var scope = services.CreateAsyncScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                    var knownMigrations = context.Database.GetMigrations().ToArray();

                    Assert.Single(knownMigrations);
                    Assert.EndsWith("_InitialCreate", knownMigrations[0], StringComparison.Ordinal);
                    Assert.Equal(knownMigrations, await context.Database.GetPendingMigrationsAsync());

                    await context.Database.MigrateAsync();

                    Assert.Equal(knownMigrations, await context.Database.GetAppliedMigrationsAsync());
                    Assert.Empty(await context.Database.GetPendingMigrationsAsync());

                    firstConnection = context.Database.GetDbConnection();
                    await context.Database.OpenConnectionAsync();
                    Assert.Equal(ConnectionState.Open, firstConnection.State);

                    await AssertMigratedSchemaAsync(firstConnection);

                    await new CategoryRepository(context, TimeProvider.System).AddAsync(category);
                    await new ProductRepository(context, TimeProvider.System).AddAsync(product);
                    await new IssuerProfileRepository(context).SaveAsync(issuer);
                    var warrantyRepository = new WarrantyRepository(context);
                    await warrantyRepository.AddAsync(saleWarranty);
                    await warrantyRepository.AddAsync(repairWarranty);
                    await warrantyRepository.AddAsync(laborOnlyWarranty);
                    await new EfUnitOfWork(context).SaveChangesAsync();

                    await new CategoryRepository(context, TimeProvider.System).UpdateAsync(
                        category.Revise("Current phones", "Changed after issuance"));
                    await new ProductRepository(context, TimeProvider.System).UpdateAsync(
                        product.Revise(
                            category.Id,
                            "Current Migration Phone",
                            product.Price,
                            "Changed after issuance"));
                    await new IssuerProfileRepository(context).SaveAsync(new IssuerProfile(
                        "Current Warranty Workshop",
                        "BE9999999999",
                        "Current Warranty Street 99"));
                    await new EfUnitOfWork(context).SaveChangesAsync();

                    await Assert.ThrowsAsync<SqliteException>(() => context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO IssuerProfile (Id, Name, Address, UpdatedAtUtc) " +
                        "VALUES (2, 'Second issuer', 'Forbidden address', '2026-11-16T00:00:00Z')"));

                    await warrantyRepository.AddAsync(Warranty.IssueForSale(
                        saleWarranty.CertificateNumber.ToLowerInvariant(),
                        WarrantyPlanId.New(),
                        imei: null,
                        saleIssuedAt,
                        new DateOnly(2026, 10, 25),
                        saleCustomer,
                        productSnapshot,
                        issuerSnapshot,
                        CoverageSnapshot.ForProductSale(new WarrantyTerm(24), "Duplicate certificate"),
                        new SaleDetails(salePurchasedOn)));
                    await Assert.ThrowsAsync<DbUpdateException>(
                        () => new EfUnitOfWork(context).SaveChangesAsync());
                }

                Assert.NotNull(firstConnection);
                Assert.Equal(ConnectionState.Closed, firstConnection.State);
            }

            await using (var services = CreateServices(databasePath))
            await using (var scope = services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                await context.Database.MigrateAsync();
                Assert.Empty(await context.Database.GetPendingMigrationsAsync());

                var storedCategory = await new CategoryRepository(context, TimeProvider.System)
                    .GetByIdAsync(category.Id);
                Assert.NotNull(storedCategory);
                Assert.Equal("Current phones", storedCategory.Name);

                var storedProduct = await new ProductRepository(context, TimeProvider.System)
                    .GetByIdAsync(product.Id);
                Assert.NotNull(storedProduct);
                Assert.Equal("Current Migration Phone", storedProduct.Name);
                Assert.Equal(1234.5678m, storedProduct.Price.Amount);
                Assert.Equal("EUR", storedProduct.Price.CurrencyCode);

                var currentIssuer = await new IssuerProfileRepository(context).GetCurrentAsync();
                Assert.NotNull(currentIssuer);
                Assert.Equal("Current Warranty Workshop", currentIssuer.Name);
                Assert.Equal("Current Warranty Street 99", currentIssuer.Address);

                var warrantyRepository = new WarrantyRepository(context);
                var storedSale = await warrantyRepository.GetByIdAsync(saleWarranty.Id);
                Assert.NotNull(storedSale);
                Assert.Equal(saleWarranty.CertificateNumber, storedSale.CertificateNumber);
                Assert.Equal(saleIssuedAt.ToUniversalTime(), storedSale.IssuedAt);
                Assert.Equal(TimeSpan.Zero, storedSale.IssuedAt.Offset);
                Assert.Equal(salePurchasedOn, storedSale.StartsOn);
                Assert.Equal(saleWarranty.ExpiresOn, storedSale.ExpiresOn);
                Assert.Equal(saleCustomer, storedSale.Customer);
                Assert.Equal(productSnapshot, storedSale.Product);
                Assert.Equal("Migration Phone at issuance", storedSale.Product.ProductName);
                Assert.Equal("Phones at issuance", storedSale.Product.CategoryName);
                Assert.Equal(1234.5678m, storedSale.Product.Price.Amount);
                Assert.Equal(issuerSnapshot, storedSale.Issuer);
                Assert.Equal("Original Warranty Workshop", storedSale.Issuer.Name);
                Assert.Equal("Original Warranty Street 10", storedSale.Issuer.Address);

                var storedRepair = await warrantyRepository.GetByIdAsync(repairWarranty.Id);
                Assert.NotNull(storedRepair?.RepairDetails);
                Assert.Equal(new DateOnly(2026, 11, 2), storedRepair.StartsOn);
                Assert.Equal("Replaced the display and battery", storedRepair.RepairDetails.WorkDescription);
                Assert.Equal(
                    ["Display module", "Battery"],
                    storedRepair.RepairDetails.ReplacedParts.Select(part => part.Description));
                Assert.Equal(
                    ["DSP-100", "BAT-200"],
                    storedRepair.RepairDetails.ReplacedParts.Select(part => part.PartNumber));

                var storedLaborOnly = await warrantyRepository.GetByIdAsync(laborOnlyWarranty.Id);
                Assert.NotNull(storedLaborOnly?.RepairDetails);
                Assert.Equal(new DateOnly(2026, 11, 3), storedLaborOnly.StartsOn);
                Assert.Equal("Labor-only board repair", storedLaborOnly.RepairDetails.WorkDescription);
                Assert.Empty(storedLaborOnly.RepairDetails.ReplacedParts);
                Assert.Equal(3, (await warrantyRepository.ListAsync()).Count);
            }
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static ServiceProvider CreateServices(string databasePath)
    {
        var services = new ServiceCollection();
        services.AddInventoryInfrastructure(databasePath, TimeZoneInfo.Utc);
        return services.BuildServiceProvider();
    }

    private static async Task AssertMigratedSchemaAsync(DbConnection connection)
    {
        var tables = await ReadColumnAsync(
            connection,
            "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name");
        Assert.Equal(
            [
                "Categories",
                "IssuerProfile",
                "Products",
                "Warranties",
                "WarrantyPlans",
                "WarrantyReplacedParts",
                "__EFMigrationsHistory",
                "__EFMigrationsLock"
            ],
            tables);

        var indexes = await ReadColumnAsync(
            connection,
            "SELECT name FROM sqlite_master WHERE type = 'index' AND name NOT LIKE 'sqlite_autoindex_%'");
        Assert.Contains("IX_Categories_ArchivedAtUtc", indexes);
        Assert.Contains("IX_Products_ArchivedAtUtc", indexes);
        Assert.Contains("IX_Products_CategoryId", indexes);
        Assert.Contains("IX_Warranties_CertificateNumber", indexes);
        Assert.Contains("IX_Warranties_IssuedAtUtcTicks", indexes);
        Assert.Contains("IX_WarrantyPlans_ArchivedAtUtc", indexes);

        var productForeignKeys = await ReadRowsAsync(
            connection,
            "SELECT \"table\", \"from\", \"to\", on_delete FROM pragma_foreign_key_list('Products')");
        Assert.Equal(["Categories|CategoryId|Id|RESTRICT"], productForeignKeys);

        var partForeignKeys = await ReadRowsAsync(
            connection,
            "SELECT \"table\", \"from\", \"to\", on_delete " +
            "FROM pragma_foreign_key_list('WarrantyReplacedParts')");
        Assert.Equal(["Warranties|WarrantyId|Id|CASCADE"], partForeignKeys);
        Assert.Empty(await ReadRowsAsync(
            connection,
            "SELECT \"table\" FROM pragma_foreign_key_list('Warranties')"));

        var warrantySql = Assert.Single(await ReadColumnAsync(
            connection,
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Warranties'"));
        Assert.Contains("CK_Warranties_DateRange", warrantySql, StringComparison.Ordinal);
        Assert.Contains("CK_Warranties_TypeSpecificDetails", warrantySql, StringComparison.Ordinal);
        Assert.Contains("CK_Warranties_CoverageScope", warrantySql, StringComparison.Ordinal);
        Assert.Contains("CK_Warranties_Imei", warrantySql, StringComparison.Ordinal);

        var issuerSql = Assert.Single(await ReadColumnAsync(
            connection,
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'IssuerProfile'"));
        Assert.Contains("CK_IssuerProfile_Singleton", issuerSql, StringComparison.Ordinal);
    }

    private static async Task<IReadOnlyList<string>> ReadColumnAsync(
        DbConnection connection,
        string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static async Task<IReadOnlyList<string>> ReadRowsAsync(
        DbConnection connection,
        string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            rows.Add(string.Join('|', Enumerable.Range(0, reader.FieldCount).Select(reader.GetString)));
        }

        return rows;
    }
}
