using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.IssuerProfiles;
using WarrantyIssueXE.Application.Products;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Infrastructure;
using WarrantyIssueXE.Infrastructure.Persistence;
using WarrantyIssueXE.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace WarrantyIssueXE.Infrastructure.Tests;

public sealed class SqliteCrudTests
{
    private const string InitialMigration = "20261111124235_InitialCreate";

    [Fact]
    public async Task Issuer_profile_uses_one_fixed_key_row()
    {
        await using var connection = await OpenInMemoryDatabaseAsync();
        var options = CreateOptions(connection);
        await using var context = new InventoryDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var repository = new IssuerProfileRepository(context);

        await repository.SaveAsync(new IssuerProfile("Contoso"));
        await context.SaveChangesAsync();
        await repository.SaveAsync(new IssuerProfile("Contoso Retail", "BE 0123"));
        await context.SaveChangesAsync();

        Assert.Equal("Contoso Retail", (await repository.GetCurrentAsync())!.Name);
        await Assert.ThrowsAsync<SqliteException>(() => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO IssuerProfile (Id, Name, Version, ModifiedOnUtc) " +
            "VALUES (2, 'Other', '00000000-0000-0000-0000-000000000000', '2026-01-01')"));
    }

    [Fact]
    public async Task Variant_without_barcodes_round_trips_and_archived_codes_remain_reserved()
    {
        await using var connection = await OpenInMemoryDatabaseAsync();
        var options = CreateOptions(connection);
        var category = new Category("Computers");
        var product = new Product(category.Id, "Laptop", new Money(1000m, "EUR"));
        var variant = new ProductVariant(product.Id, "Black");

        await using (var context = new InventoryDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            await new CategoryRepository(context, TimeProvider.System).AddAsync(category);
            await new ProductRepository(context, TimeProvider.System).AddAsync(product);
            await new ProductVariantRepository(context, TimeProvider.System).AddAsync(variant);
            await context.SaveChangesAsync();
        }

        await using (var context = new InventoryDbContext(options))
        {
            var repository = new ProductVariantRepository(context, TimeProvider.System);
            var stored = await repository.GetByIdAsync(variant.Id);
            Assert.NotNull(stored);
            Assert.Empty(stored.Barcodes);

            await repository.UpdateAsync(stored.AddBarcode(new ProductBarcode("000123")));
            await context.SaveChangesAsync();
            await repository.ArchiveAsync(variant.Id);
            await context.SaveChangesAsync();
        }

        await using (var context = new InventoryDbContext(options))
        {
            var repository = new ProductVariantRepository(context, TimeProvider.System);
            Assert.Empty(await repository.ListByProductIdAsync(product.Id));
            var reserved = await repository.FindReservedBarcodeCodesAsync(["000123"]);
            Assert.Contains("000123", reserved);
        }
    }

    [Fact]
    public async Task Duplicate_barcode_unique_constraint_becomes_a_stable_application_conflict()
    {
        await using var connection = await OpenInMemoryDatabaseAsync();
        var options = CreateOptions(connection);
        var category = new Category("Computers");
        var product = new Product(category.Id, "Laptop", new Money(1000m, "EUR"));
        var first = new ProductVariant(product.Id, "Black");
        var second = new ProductVariant(product.Id, "Silver");

        await using (var setup = new InventoryDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            await new CategoryRepository(setup, TimeProvider.System).AddAsync(category);
            await new ProductRepository(setup, TimeProvider.System).AddAsync(product);
            await new ProductVariantRepository(setup, TimeProvider.System).AddAsync(first);
            await new ProductVariantRepository(setup, TimeProvider.System).AddAsync(second);
            await setup.SaveChangesAsync();
        }

        await using var firstContext = new InventoryDbContext(options);
        await using var secondContext = new InventoryDbContext(options);
        var firstRepository = new ProductVariantRepository(firstContext, TimeProvider.System);
        var secondRepository = new ProductVariantRepository(secondContext, TimeProvider.System);
        await firstRepository.UpdateAsync((await firstRepository.GetByIdAsync(first.Id))!
            .AddBarcode(new ProductBarcode("RACE-001")));
        await secondRepository.UpdateAsync((await secondRepository.GetByIdAsync(second.Id))!
            .AddBarcode(new ProductBarcode("RACE-001")));

        await new EfUnitOfWork(firstContext).SaveChangesAsync();
        var exception = await Assert.ThrowsAsync<ProductBarcodeConflictException>(
            () => new EfUnitOfWork(secondContext).SaveChangesAsync());

        Assert.Equal("RACE-001", exception.BarcodeCode);
    }

    [Fact]
    public async Task Concurrent_issuer_profile_updates_are_rejected()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"inventory-profile-{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection();
            services.AddInventoryInfrastructure(databasePath, TimeZoneInfo.Utc);
            await using var provider = services.BuildServiceProvider();

            await using (var setupScope = provider.CreateAsyncScope())
            {
                var factory = setupScope.ServiceProvider.GetRequiredService<IDbContextFactory<InventoryDbContext>>();
                await using var setupContext = await factory.CreateDbContextAsync();
                await setupContext.Database.EnsureCreatedAsync();
                await setupScope.ServiceProvider.GetRequiredService<IIssuerProfileStore>()
                    .SaveAsync(new IssuerProfile("Original"));
                await setupScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            }

            await using var firstScope = provider.CreateAsyncScope();
            await using var secondScope = provider.CreateAsyncScope();
            await firstScope.ServiceProvider.GetRequiredService<IIssuerProfileStore>()
                .SaveAsync(new IssuerProfile("First update"));
            await secondScope.ServiceProvider.GetRequiredService<IIssuerProfileStore>()
                .SaveAsync(new IssuerProfile("Second update"));

            await firstScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            await Assert.ThrowsAsync<IssuerProfileConflictException>(
                () => secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Concurrent_first_time_issuer_profile_creation_is_rejected_as_a_stable_conflict()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"inventory-profile-create-{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection();
            services.AddInventoryInfrastructure(databasePath, TimeZoneInfo.Utc);
            await using var provider = services.BuildServiceProvider();

            await using (var setupScope = provider.CreateAsyncScope())
            {
                var factory = setupScope.ServiceProvider.GetRequiredService<IDbContextFactory<InventoryDbContext>>();
                await using var setupContext = await factory.CreateDbContextAsync();
                await setupContext.Database.EnsureCreatedAsync();
            }

            await using var firstScope = provider.CreateAsyncScope();
            await using var secondScope = provider.CreateAsyncScope();
            await firstScope.ServiceProvider.GetRequiredService<IIssuerProfileStore>()
                .SaveAsync(new IssuerProfile("First setup"));
            await secondScope.ServiceProvider.GetRequiredService<IIssuerProfileStore>()
                .SaveAsync(new IssuerProfile("Second setup"));

            await firstScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            await Assert.ThrowsAsync<IssuerProfileConflictException>(
                () => secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Migration_from_empty_legacy_database_creates_an_unconfigured_singleton_store()
    {
        await using var connection = await OpenInMemoryDatabaseAsync();
        await using var context = new InventoryDbContext(CreateOptions(connection));
        await context.GetService<IMigrator>().MigrateAsync(InitialMigration);

        await context.Database.MigrateAsync();

        Assert.Equal(1L, await ScalarAsync<long>(connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'IssuerProfile'"));
        Assert.Equal(0L, await ScalarAsync<long>(connection, "SELECT COUNT(*) FROM IssuerProfile"));
        Assert.Equal(0L, await ScalarAsync<long>(connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Companies'"));
    }

    [Fact]
    public async Task Migration_copies_one_active_retailer_and_preserves_historical_warranty_facts()
    {
        await using var connection = await OpenInMemoryDatabaseAsync();
        await using var context = new InventoryDbContext(CreateOptions(connection));
        await context.GetService<IMigrator>().MigrateAsync(InitialMigration);

        var companyId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var warrantyId = Guid.NewGuid();
        var version = Guid.NewGuid();
        var timestamp = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Companies
                (Id, Name, VatNumber, Address, Email, Phone, Version, CreatedOnUtc, ModifiedOnUtc, DeletedOnUtc)
            VALUES
                ({companyId}, {"Current issuer"}, {"BE-CURRENT"}, {"Current address"}, {"current@example.test"}, {"+32000"},
                 {version}, {timestamp}, {timestamp}, NULL)
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO CompanyRoles (CompanyId, Role) VALUES ({companyId}, {"Retailer"})
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Categories (Id, Name, Version, CreatedOnUtc, ModifiedOnUtc)
            VALUES ({categoryId}, {"Computers"}, {version}, {timestamp}, {timestamp})
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Products
                (Id, CategoryId, Name, PriceAmount, PriceCurrencyCode, Version, CreatedOnUtc, ModifiedOnUtc)
            VALUES ({productId}, {categoryId}, {"Laptop"}, {1000m}, {"EUR"}, {version}, {timestamp}, {timestamp})
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO ProductVariants
                (Id, ProductId, Name, Version, CreatedOnUtc, ModifiedOnUtc)
            VALUES ({variantId}, {productId}, {"Black"}, {version}, {timestamp}, {timestamp})
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO WarrantyPlans
                (Id, CompanyId, Type, Name, TermMonths, CoverageDescription, Version, CreatedOnUtc, ModifiedOnUtc)
            VALUES ({planId}, {companyId}, {"ProductSale"}, {"Standard"}, {24}, {"Parts and labour"},
                    {version}, {timestamp}, {timestamp})
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Warranties
                (Id, CertificateNumber, Type, Status, ProductVariantId, WarrantyPlanId, IssuedByRetailerId,
                 IssuedAtUtcTicks, StartsOn, ExpiresOn, CustomerFullName, ProductName, ProductVariantName,
                 RetailerName, RetailerVatNumber, RetailerAddress, RetailerEmail, RetailerPhone,
                 CoverageTermMonths, CoverageScope, CoverageDescription, CoverageScopeStatement,
                 PurchasedOn, PurchasePriceAmount, PurchasePriceCurrencyCode)
            VALUES
                ({warrantyId}, {"WR-HISTORICAL"}, {"ProductSale"}, {"Issued"}, {variantId}, {planId}, {companyId},
                 {timestamp.Ticks}, {new DateOnly(2026, 1, 1)}, {new DateOnly(2027, 12, 31)}, {"Ada"}, {"Laptop"}, {"Black"},
                 {"Historical issuer"}, {"BE-HISTORICAL"}, {"Old address"}, {"old@example.test"}, {"+32111"},
                 {24}, {"Product"}, {"Parts and labour"}, {"Product coverage"},
                 {new DateOnly(2026, 1, 1)}, {1000m}, {"EUR"})
            """);

        await context.Database.MigrateAsync();

        Assert.Equal("Current issuer", await ScalarAsync<string>(connection, "SELECT Name FROM IssuerProfile WHERE Id = 1"));
        Assert.Equal("Historical issuer", await ScalarAsync<string>(connection, "SELECT IssuerName FROM Warranties WHERE CertificateNumber = 'WR-HISTORICAL'"));
        Assert.Equal("BE-HISTORICAL", await ScalarAsync<string>(connection, "SELECT IssuerVatNumber FROM Warranties WHERE CertificateNumber = 'WR-HISTORICAL'"));
        Assert.Equal(planId.ToString().ToUpperInvariant(),
            (await ScalarAsync<string>(connection, "SELECT Id FROM WarrantyPlans WHERE Name = 'Standard'")).ToUpperInvariant());
        Assert.Equal(0L, await ScalarAsync<long>(connection,
            "SELECT COUNT(*) FROM pragma_table_info('WarrantyPlans') WHERE name = 'CompanyId'"));
    }

    [Fact]
    public async Task Migration_stops_before_destructive_changes_when_legacy_companies_are_ambiguous()
    {
        await using var connection = await OpenInMemoryDatabaseAsync();
        await using var context = new InventoryDbContext(CreateOptions(connection));
        await context.GetService<IMigrator>().MigrateAsync(InitialMigration);
        var timestamp = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        foreach (var name in new[] { "First", "Second" })
        {
            var companyId = Guid.NewGuid();
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO Companies
                    (Id, Name, Version, CreatedOnUtc, ModifiedOnUtc, DeletedOnUtc)
                VALUES ({companyId}, {name}, {Guid.NewGuid()}, {timestamp}, {timestamp}, NULL)
                """);
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO CompanyRoles (CompanyId, Role) VALUES ({companyId}, {"Retailer"})
                """);
        }

        var exception = await Assert.ThrowsAsync<SqliteException>(() => context.Database.MigrateAsync());

        Assert.Contains("legacy Company data or ownership is ambiguous", exception.Message, StringComparison.Ordinal);
        Assert.Equal(2L, await ScalarAsync<long>(connection, "SELECT COUNT(*) FROM Companies"));
        Assert.Equal(0L, await ScalarAsync<long>(connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'IssuerProfile'"));
    }

    private static async Task<SqliteConnection> OpenInMemoryDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static DbContextOptions<InventoryDbContext> CreateOptions(SqliteConnection connection) =>
        new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(connection).Options;

    private static async Task<T> ScalarAsync<T>(SqliteConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        var value = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(value!, typeof(T));
    }
}
