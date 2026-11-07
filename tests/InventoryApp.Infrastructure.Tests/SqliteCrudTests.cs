using InventoryApp.Domain.Entities;
using InventoryApp.Domain.ValueObjects;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Tests;

public sealed class SqliteCrudTests
{
    [Fact]
    public async Task Sqlite_round_trips_all_aggregates_through_persistence_models()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new InventoryDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var unitOfWork = new EfUnitOfWork(db);
        var categories = new CategoryRepository(db);
        var companies = new CompanyRepository(db);
        var products = new ProductRepository(db);
        var variants = new ProductVariantRepository(db);
        var services = new ServiceRepository(db);
        var warranties = new WarrantyRepository(db);

        var category = new Category("Cables");
        var replacementCategory = new Category("Accessories");
        await categories.AddAsync(category);
        await categories.AddAsync(replacementCategory);

        var company = new Company(
            "Acme Repairs",
            new CompanyContact(
                "Avenue des Arts 1",
                "hello@acme.example",
                "+32 2 555 0100"));
        await companies.AddAsync(company);

        var service = new Service(
            company.Id,
            "New-sale coverage",
            new WarrantyTerm(24),
            "New-sale warranty service");
        await services.AddAsync(service);

        var product = new Product(
            category.Id,
            "USB-C Cable",
            new Money(12.50m, "EUR"),
            "Fast charging");
        await products.AddAsync(product);

        var variant = new ProductVariant(
            product.Id,
            "1 metre",
            "CAB-1M",
            [new ProductBarcode("123", "Retail box")]);
        await variants.AddAsync(variant);

        var warranty = Warranty.Issue(
            product.Id,
            service.Id,
            "Two-year coverage",
            new DateOnly(2026, 1, 1),
            new WarrantyTerm(24));
        await warranties.AddAsync(warranty);

        var additionalWarranty = Warranty.Issue(
            product.Id,
            service.Id,
            "Additional two-year coverage",
            new DateOnly(2026, 1, 1),
            service.WarrantyTerm);
        await warranties.AddAsync(additionalWarranty);

        await unitOfWork.SaveChangesAsync();

        var loadedProduct = await products.GetByIdAsync(product.Id);
        var loadedVariant = await variants.GetByIdAsync(variant.Id);
        var loadedService = await services.GetByIdAsync(service.Id);
        var loadedWarranty = await warranties.GetByIdAsync(warranty.Id);
        var loadedAdditionalWarranty = await warranties.GetByIdAsync(additionalWarranty.Id);

        Assert.Equal(product.Price, loadedProduct!.Price);
        Assert.Equal(category.Id, loadedProduct.CategoryId);
        Assert.Equal("123", loadedVariant!.Barcodes.Single().Code);
        Assert.Equal(service.CompanyId, loadedService!.CompanyId);
        Assert.Equal(company.Contact, (await companies.GetByIdAsync(company.Id))!.Contact);
        Assert.Equal(24, loadedService.WarrantyTerm.Months);
        Assert.Equal(warranty.ProductId, loadedWarranty!.ProductId);
        Assert.Equal(warranty.ServiceId, loadedWarranty.ServiceId);
        Assert.Equal(warranty.Description, loadedWarranty.Description);
        Assert.Equal(additionalWarranty.Description, loadedAdditionalWarranty!.Description);

        var updatedProduct = loadedProduct.Revise(
            replacementCategory.Id,
            "USB-C Cable Premium",
            loadedProduct.Price,
            loadedProduct.Description);
        await products.UpdateAsync(updatedProduct);

        var updatedVariant = loadedVariant.AddBarcode(new ProductBarcode("456"));
        await variants.UpdateAsync(updatedVariant);

        await unitOfWork.SaveChangesAsync();

        Assert.Equal(
            "USB-C Cable Premium",
            (await products.GetByIdAsync(product.Id))!.Name);
        Assert.Equal(
            replacementCategory.Id,
            (await products.GetByIdAsync(product.Id))!.CategoryId);
        Assert.Equal(
            2,
            (await variants.GetByIdAsync(variant.Id))!.Barcodes.Count);

        await warranties.ArchiveAsync(warranty.Id);
        await warranties.ArchiveAsync(additionalWarranty.Id);
        await unitOfWork.SaveChangesAsync();

        Assert.Null(await warranties.GetByIdAsync(warranty.Id));
        Assert.Null(await warranties.GetByIdAsync(additionalWarranty.Id));

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Warranties WHERE DeletedOnUtc IS NOT NULL";

        var historicalWarrantyCount = (long)(await command.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(2L, historicalWarrantyCount);
    }

    [Fact]
    public async Task Sqlite_enforces_global_barcode_uniqueness()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new InventoryDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var unitOfWork = new EfUnitOfWork(db);
        var categories = new CategoryRepository(db);
        var products = new ProductRepository(db);
        var variants = new ProductVariantRepository(db);
        var category = new Category("Cables");
        var product = new Product(category.Id, "USB-C Cable", new Money(12.50m, "EUR"));
        var firstVariant = new ProductVariant(product.Id, "1 metre", barcodes: [new ProductBarcode("123")]);

        await categories.AddAsync(category);
        await products.AddAsync(product);
        await variants.AddAsync(firstVariant);
        await unitOfWork.SaveChangesAsync();

        var duplicateBarcodeVariant = new ProductVariant(
            product.Id,
            "2 metres",
            barcodes: [new ProductBarcode("123")]);
        await variants.AddAsync(duplicateBarcodeVariant);

        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task Sqlite_archives_records_and_excludes_them_from_normal_reads()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new InventoryDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var unitOfWork = new EfUnitOfWork(db);
        var categories = new CategoryRepository(db);
        var companies = new CompanyRepository(db);
        var products = new ProductRepository(db);
        var variants = new ProductVariantRepository(db);
        var services = new ServiceRepository(db);
        var category = new Category("Cables");
        var product = new Product(category.Id, "USB-C Cable", new Money(12.50m, "EUR"));
        var variant = new ProductVariant(
            product.Id,
            "1 metre",
            barcodes: [new ProductBarcode("123")]);
        var company = new Company("Acme Repairs", CompanyContact.Empty);
        var service = new Service(company.Id, "Repair", new WarrantyTerm(6));

        await categories.AddAsync(category);
        await companies.AddAsync(company);
        await products.AddAsync(product);
        await variants.AddAsync(variant);
        await services.AddAsync(service);
        await unitOfWork.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await categories.ArchiveAsync(category.Id);
        await companies.ArchiveAsync(company.Id);
        await products.ArchiveAsync(product.Id);
        await variants.ArchiveAsync(variant.Id);
        await services.ArchiveAsync(service.Id);
        await unitOfWork.SaveChangesAsync();

        Assert.Null(await categories.GetByIdAsync(category.Id));
        Assert.Null(await companies.GetByIdAsync(company.Id));
        Assert.Null(await products.GetByIdAsync(product.Id));
        Assert.Null(await variants.GetByIdAsync(variant.Id));
        Assert.Null(await services.GetByIdAsync(service.Id));
        Assert.Null(await variants.FindByBarcodeAsync(new ProductBarcode("123")));
        Assert.True(await variants.IsBarcodeReservedAsync(new ProductBarcode("123")));

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM Categories WHERE DeletedOnUtc IS NOT NULL) +
                (SELECT COUNT(*) FROM Companies WHERE DeletedOnUtc IS NOT NULL) +
                (SELECT COUNT(*) FROM Products WHERE DeletedOnUtc IS NOT NULL) +
                (SELECT COUNT(*) FROM ProductVariants WHERE DeletedOnUtc IS NOT NULL) +
                (SELECT COUNT(*) FROM Services WHERE DeletedOnUtc IS NOT NULL)
            """;

        var archivedRecordCount = (long)(await command.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(5L, archivedRecordCount);
    }
}
