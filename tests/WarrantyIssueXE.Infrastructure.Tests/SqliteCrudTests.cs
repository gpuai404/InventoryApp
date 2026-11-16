using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Domain.WarrantyDetails;
using WarrantyIssueXE.Infrastructure.Persistence;
using WarrantyIssueXE.Infrastructure.Persistence.Repositories;

namespace WarrantyIssueXE.Infrastructure.Tests;

public sealed class SqliteCrudTests
{
    [Fact]
    public void Dependency_injection_registers_each_persistence_contract_once()
    {
        var services = new ServiceCollection();

        services.AddInventoryInfrastructure("inventory.db", TimeZoneInfo.Utc);

        Type[] persistenceContracts =
        [
            typeof(ICategoryReader),
            typeof(ICategoryWriter),
            typeof(IProductReader),
            typeof(IProductWriter),
            typeof(IIssuerProfileStore),
            typeof(IWarrantyPlanReader),
            typeof(IWarrantyPlanWriter),
            typeof(IWarrantyReader),
            typeof(IWarrantyWriter),
            typeof(IUnitOfWork)
        ];
        foreach (var contract in persistenceContracts)
        {
            Assert.Single(services, descriptor => descriptor.ServiceType == contract);
        }
    }

    [Fact]
    public async Task Schema_contains_the_catalog_and_warranty_model()
    {
        await using var database = await TestDatabase.CreateAsync();

        var tables = await database.ReadNamesAsync(
            "SELECT name FROM sqlite_master WHERE type = 'table'");
        var warrantyColumns = await database.ReadNamesAsync("SELECT name FROM pragma_table_info('Warranties')");
        var replacedPartColumns = await database.ReadNamesAsync(
            "SELECT name FROM pragma_table_info('WarrantyReplacedParts')");

        Assert.Contains("Categories", tables);
        Assert.Contains("Products", tables);
        Assert.Contains("WarrantyPlans", tables);
        Assert.Contains("Warranties", tables);
        Assert.Contains("WarrantyReplacedParts", tables);

        Assert.Contains("ProductId", warrantyColumns);
        Assert.Contains("ProductCategoryId", warrantyColumns);
        Assert.Contains("ProductCategoryName", warrantyColumns);
        Assert.Contains("ProductPriceAmount", warrantyColumns);
        Assert.Contains("ProductPriceCurrencyCode", warrantyColumns);
        Assert.Contains("Imei", warrantyColumns);
        Assert.Equal(["WarrantyId", "Position", "Description", "PartNumber"], replacedPartColumns);
    }

    [Fact]
    public async Task Issuer_profile_requires_one_fixed_key_row_and_an_address()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new IssuerProfileRepository(database.Context);

        await repository.SaveAsync(new IssuerProfile(
            "Contoso",
            "BE 0123",
            "1 Main Street",
            "issuer@example.test",
            "+32000"));
        await database.Context.SaveChangesAsync();

        var stored = await repository.GetCurrentAsync();
        Assert.NotNull(stored);
        Assert.Equal("1 Main Street", stored.Address);
        await Assert.ThrowsAsync<SqliteException>(() => database.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO IssuerProfile (Id, Name, Address, UpdatedAtUtc) " +
            "VALUES (2, 'Other', 'Other address', '2026-01-01T00:00:00Z')"));
        await Assert.ThrowsAsync<SqliteException>(() => database.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO IssuerProfile (Id, Name, Address, UpdatedAtUtc) " +
            "VALUES (1, 'Missing address', NULL, '2026-01-01T00:00:00Z')"));
    }

    [Fact]
    public async Task Product_money_and_category_round_trip_and_archive_filter_is_applied()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = new Category("Mobile devices", "Phones and tablets");
        var product = new Product(
            category.Id,
            "Precision Phone",
            new Money(1234.5678m, "eur"),
            "Current catalog description");

        await new CategoryRepository(database.Context, TimeProvider.System).AddAsync(category);
        await new ProductRepository(database.Context, TimeProvider.System).AddAsync(product);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var repository = new ProductRepository(database.Context, TimeProvider.System);
        var stored = await repository.GetByIdAsync(product.Id);
        Assert.NotNull(stored);
        Assert.Equal(product.Id, stored.Id);
        Assert.Equal(category.Id, stored.CategoryId);
        Assert.Equal(1234.5678m, stored.Price.Amount);
        Assert.Equal("EUR", stored.Price.CurrencyCode);

        await repository.ArchiveAsync(product.Id);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        Assert.Null(await repository.GetByIdAsync(product.Id));
        Assert.DoesNotContain(await repository.ListAsync(), item => item.Id == product.Id);
    }

    [Fact]
    public async Task Warranty_plan_round_trips_revises_without_changing_workflow_and_archives()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new WarrantyPlanRepository(database.Context, TimeProvider.System);
        var plan = new WarrantyPlan(
            WarrantyType.ProductSale,
            "Standard sale plan",
            new WarrantyTerm(24),
            "Manufacturing defects");

        await repository.AddAsync(plan);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var stored = await repository.GetByIdAsync(plan.Id);
        Assert.NotNull(stored);
        Assert.Equal(WarrantyType.ProductSale, stored.Type);
        Assert.Equal(24, stored.Term.Months);

        var revised = stored.Revise("Extended sale plan", new WarrantyTerm(36), "Extended defects coverage");
        await repository.UpdateAsync(revised);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        stored = await repository.GetByIdAsync(plan.Id);
        Assert.NotNull(stored);
        Assert.Equal(WarrantyType.ProductSale, stored.Type);
        Assert.Equal("Extended sale plan", stored.Name);
        Assert.Equal(36, stored.Term.Months);

        var incompatible = new WarrantyPlan(
            WarrantyType.Repair,
            stored.Name,
            stored.Term,
            stored.CoverageDescription,
            stored.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(incompatible));

        await repository.ArchiveAsync(plan.Id);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        Assert.Null(await repository.GetByIdAsync(plan.Id));
    }

    [Fact]
    public async Task Sale_warranty_round_trips_all_historical_facts_without_live_master_records()
    {
        await using var database = await TestDatabase.CreateAsync();
        var warranty = CreateSaleWarranty("sale-0001");

        await new WarrantyRepository(database.Context).AddAsync(warranty);
        await new EfUnitOfWork(database.Context).SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var stored = await new WarrantyRepository(database.Context).GetByIdAsync(warranty.Id);
        Assert.NotNull(stored);
        Assert.Equal(warranty.Id, stored.Id);
        Assert.Equal("SALE-0001", stored.CertificateNumber);
        Assert.Equal(warranty.IssuedAt, stored.IssuedAt);
        Assert.Equal(TimeSpan.Zero, stored.IssuedAt.Offset);
        Assert.Equal(warranty.StartsOn, stored.StartsOn);
        Assert.Equal(warranty.ExpiresOn, stored.ExpiresOn);
        Assert.Equal(warranty.Product, stored.Product);
        Assert.Equal(1499.995m, stored.Product.Price.Amount);
        Assert.Equal("EUR", stored.Product.Price.CurrencyCode);
        Assert.Equal(warranty.Imei, stored.Imei);
        Assert.Equal(warranty.Customer, stored.Customer);
        Assert.Equal(warranty.Issuer, stored.Issuer);
        Assert.Equal("Warranty Street 10", stored.Issuer.Address);
        Assert.Equal(warranty.SaleDetails, stored.SaleDetails);
        Assert.Null(stored.RepairDetails);
        Assert.Equal(warranty.Coverage, stored.Coverage);

        Assert.Equal(0L, await database.ScalarAsync<long>("SELECT COUNT(*) FROM Products"));
        Assert.Equal(0L, await database.ScalarAsync<long>("SELECT COUNT(*) FROM Categories"));
        Assert.Equal(0L, await database.ScalarAsync<long>("SELECT COUNT(*) FROM WarrantyPlans"));
        Assert.Equal(0L, await database.ScalarAsync<long>("SELECT COUNT(*) FROM IssuerProfile"));
    }

    [Fact]
    public async Task Repair_warranties_support_labor_only_and_preserve_replaced_part_order()
    {
        await using var database = await TestDatabase.CreateAsync();
        var laborOnly = CreateRepairWarranty("REPAIR-LABOR");
        var withParts = CreateRepairWarranty(
            "REPAIR-PARTS",
            [
                new ReplacedPartSnapshot("Display module", "DSP-100"),
                new ReplacedPartSnapshot("Water seal"),
                new ReplacedPartSnapshot("Battery", "BAT-200")
            ]);
        var repository = new WarrantyRepository(database.Context);

        await repository.AddAsync(laborOnly);
        await repository.AddAsync(withParts);
        await new EfUnitOfWork(database.Context).SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var storedLaborOnly = await repository.GetByCertificateNumberAsync("repair-labor");
        var storedWithParts = await repository.GetByCertificateNumberAsync("REPAIR-PARTS");
        Assert.NotNull(storedLaborOnly?.RepairDetails);
        Assert.Empty(storedLaborOnly.RepairDetails.ReplacedParts);
        Assert.NotNull(storedWithParts?.RepairDetails);
        Assert.Equal("Completed board-level repair", storedWithParts.RepairDetails.WorkDescription);
        Assert.Equal(
            ["Display module", "Water seal", "Battery"],
            storedWithParts.RepairDetails.ReplacedParts.Select(part => part.Description));
        Assert.Equal(["DSP-100", null, "BAT-200"],
            storedWithParts.RepairDetails.ReplacedParts.Select(part => part.PartNumber));
        Assert.Equal(CoverageSnapshot.RepairScopeStatement, storedWithParts.Coverage.ScopeStatement);
        Assert.Equal(withParts.Coverage, storedWithParts.Coverage);
        Assert.Equal(2, (await repository.ListAsync()).Count);
        await Assert.ThrowsAsync<SqliteException>(() => database.Context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO WarrantyReplacedParts (WarrantyId, Position, Description)
            VALUES ({withParts.Id.Value}, {0}, {"Duplicate position"})
            """));
    }

    [Fact]
    public async Task Warranty_constraints_reject_duplicate_numbers_and_contradictory_workflow_columns()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new WarrantyRepository(database.Context);
        var first = CreateSaleWarranty("UNIQUE-001");

        await repository.AddAsync(first);
        await new EfUnitOfWork(database.Context).SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await repository.AddAsync(CreateSaleWarranty("unique-001"));
        await Assert.ThrowsAsync<DbUpdateException>(
            () => new EfUnitOfWork(database.Context).SaveChangesAsync());
        database.Context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<SqliteException>(() => database.Context.Database.ExecuteSqlRawAsync(
            "UPDATE Warranties SET RepairedOn = '2026-01-01' WHERE CertificateNumber = 'UNIQUE-001'"));
        await Assert.ThrowsAsync<SqliteException>(() => database.Context.Database.ExecuteSqlRawAsync(
            "UPDATE Warranties SET ProductPriceAmount = '-1' WHERE CertificateNumber = 'UNIQUE-001'"));
        await Assert.ThrowsAsync<SqliteException>(() => database.Context.Database.ExecuteSqlRawAsync(
            "UPDATE Warranties SET Imei = '123' WHERE CertificateNumber = 'UNIQUE-001'"));
        await Assert.ThrowsAsync<SqliteException>(() => database.Context.Database.ExecuteSqlRawAsync(
            "UPDATE Warranties SET ProductCategoryName = NULL WHERE CertificateNumber = 'UNIQUE-001'"));
    }

    [Fact]
    public async Task Persistence_reconstitution_rejects_invalid_expiry_and_invalid_imei()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new WarrantyRepository(database.Context);
        var invalidExpiry = CreateSaleWarranty("BAD-EXPIRY");
        var invalidImei = CreateSaleWarranty("BAD-IMEI");

        await repository.AddAsync(invalidExpiry);
        await repository.AddAsync(invalidImei);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await database.Context.Database.ExecuteSqlRawAsync(
            "UPDATE Warranties SET ExpiresOn = '2030-01-01' WHERE CertificateNumber = 'BAD-EXPIRY'");
        await database.Context.Database.ExecuteSqlRawAsync(
            "UPDATE Warranties SET Imei = '490154203237519' WHERE CertificateNumber = 'BAD-IMEI'");

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => repository.GetByCertificateNumberAsync("BAD-EXPIRY"));
        await Assert.ThrowsAsync<DomainValidationException>(
            () => repository.GetByCertificateNumberAsync("BAD-IMEI"));
    }

    [Fact]
    public async Task Add_is_staged_until_unit_of_work_saves_and_reads_do_not_duplicate_warranties()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new WarrantyRepository(database.Context);
        var warranty = CreateSaleWarranty("STAGED-001");

        await repository.AddAsync(warranty);
        Assert.Equal(0L, await database.ScalarAsync<long>("SELECT COUNT(*) FROM Warranties"));

        await new EfUnitOfWork(database.Context).SaveChangesAsync();
        Assert.Equal(1L, await database.ScalarAsync<long>("SELECT COUNT(*) FROM Warranties"));

        Assert.NotNull(await repository.GetByIdAsync(warranty.Id));
        Assert.NotNull(await repository.GetByCertificateNumberAsync(warranty.CertificateNumber));
        Assert.Equal(1L, await database.ScalarAsync<long>("SELECT COUNT(*) FROM Warranties"));
    }

    [Fact]
    public async Task Archived_product_is_hidden_while_its_historical_warranty_remains_readable()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = new Category("Phones");
        var product = new Product(category.Id, "Archive Phone", new Money(800m, "EUR"));
        var productRepository = new ProductRepository(database.Context, TimeProvider.System);
        var warrantyRepository = new WarrantyRepository(database.Context);
        var warranty = CreateSaleWarranty("ARCHIVE-001", product.ToSnapshot(category.Name));

        await new CategoryRepository(database.Context, TimeProvider.System).AddAsync(category);
        await productRepository.AddAsync(product);
        await warrantyRepository.AddAsync(warranty);
        await database.Context.SaveChangesAsync();

        await productRepository.ArchiveAsync(product.Id);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        Assert.Null(await productRepository.GetByIdAsync(product.Id));
        var historical = await warrantyRepository.GetByIdAsync(warranty.Id);
        Assert.NotNull(historical);
        Assert.Equal(product.Id, historical.Product.ProductId);
        Assert.Equal("Archive Phone", historical.Product.ProductName);
    }

    private static Warranty CreateSaleWarranty(string certificateNumber, ProductSnapshot? product = null)
    {
        var issuedAt = new DateTimeOffset(2026, 3, 15, 12, 30, 45, TimeSpan.FromHours(1));
        var purchasedOn = new DateOnly(2026, 3, 14);

        return Warranty.IssueForSale(
            certificateNumber,
            new WarrantyPlanId(Guid.NewGuid()),
            new Imei("490154203237518"),
            issuedAt,
            new CustomerSnapshot("Ada Lovelace", "ada@example.test", "+32123", "Customer Lane 5"),
            product ?? CreateProductSnapshot(),
            CreateIssuerSnapshot(),
            CoverageSnapshot.ForProductSale(new WarrantyTerm(24), "Manufacturing defects"),
            new SaleDetails(purchasedOn));
    }

    private static Warranty CreateRepairWarranty(
        string certificateNumber,
        IReadOnlyList<ReplacedPartSnapshot>? parts = null)
    {
        var issuedAt = new DateTimeOffset(2026, 4, 20, 16, 15, 0, TimeSpan.FromHours(2));
        var repairedOn = new DateOnly(2026, 4, 20);

        return Warranty.IssueForRepair(
            certificateNumber,
            warrantyPlanId: null,
            imei: null,
            issuedAt,
            new CustomerSnapshot("Grace Hopper", "grace@example.test"),
            CreateProductSnapshot(),
            CreateIssuerSnapshot(),
            CoverageSnapshot.ForRepair(new WarrantyTerm(6), "Completed repair workmanship"),
            new RepairDetails(repairedOn, "Completed board-level repair", parts));
    }

    private static ProductSnapshot CreateProductSnapshot() =>
        new(
            ProductId.New(),
            "Precision Phone",
            CategoryId.New(),
            "Mobile devices",
            new Money(1499.995m, "EUR"));

    private static IssuerSnapshot CreateIssuerSnapshot() =>
        new(
            "Warranty Workshop",
            "BE0123456789",
            "Warranty Street 10",
            "support@example.test",
            "+329999999");

    private sealed class TestDatabase : IAsyncDisposable
    {
        private TestDatabase(SqliteConnection connection, InventoryDbContext context)
        {
            Connection = connection;
            Context = context;
        }

        public SqliteConnection Connection { get; }

        public InventoryDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new InventoryDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new TestDatabase(connection, context);
        }

        public async Task<IReadOnlyList<string>> ReadNamesAsync(string commandText)
        {
            await using var command = Connection.CreateCommand();
            command.CommandText = commandText;
            await using var reader = await command.ExecuteReaderAsync();
            var names = new List<string>();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }

            return names;
        }

        public async Task<T> ScalarAsync<T>(string commandText)
        {
            await using var command = Connection.CreateCommand();
            command.CommandText = commandText;
            var value = await command.ExecuteScalarAsync();
            return (T)Convert.ChangeType(value!, typeof(T));
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
