using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarrantyIssueXE.Application.Categories;
using WarrantyIssueXE.Application.IssuerProfiles;
using WarrantyIssueXE.Application.Products;
using WarrantyIssueXE.Application.Warranties;
using WarrantyIssueXE.Application.WarrantyPlans;
using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Infrastructure.Persistence;

namespace WarrantyIssueXE.Infrastructure.Tests;

public sealed class ApplicationPersistenceIntegrationTests
{
    [Fact]
    public async Task Sale_and_repair_services_persist_and_reload_complete_historical_snapshots()
    {
        await using var database = await MigratedDatabase.CreateAsync();
        Guid productId;
        WarrantyDto issuedSale;
        WarrantyDto issuedRepair;

        await using (var scope = database.Services.CreateAsyncScope())
        {
            var categories = scope.ServiceProvider.GetRequiredService<CategoryCommandService>();
            var products = scope.ServiceProvider.GetRequiredService<ProductCommandService>();
            var issuer = scope.ServiceProvider.GetRequiredService<IssuerProfileService>();
            var warranties = scope.ServiceProvider.GetRequiredService<WarrantyCommandService>();
            var plans = scope.ServiceProvider.GetRequiredService<WarrantyPlanCommandService>();

            var category = await categories.CreateAsync(new CreateCategoryRequest(
                "Phones at issuance",
                "Original category description"));
            var product = await products.CreateAsync(new CreateProductRequest(
                category.Id,
                "Phone at issuance",
                1234.5678m,
                "eur",
                "Original product description"));
            productId = product.Id;
            await issuer.SaveAsync(new SaveIssuerProfileRequest(
                "Issuer at issuance",
                "BE0123456789",
                "Original Warranty Street 10",
                "original@example.test",
                "+321111111"));
            var salePlan = await plans.CreateAsync(new CreateWarrantyPlanRequest(
                WarrantyType.ProductSale,
                "Standard product sale",
                24,
                "Manufacturing defects"));
            var repairPlan = await plans.CreateAsync(new CreateWarrantyPlanRequest(
                WarrantyType.Repair,
                "Standard repair",
                6,
                "Repair workmanship"));

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            issuedSale = await warranties.IssueProductSaleWarrantyAsync(
                new IssueProductSaleWarrantyRequest(
                    product.Id,
                    salePlan.Id,
                    "490154203237518",
                    new CustomerInput(
                        "Ada Lovelace",
                        "ada@example.test",
                        "+322222222",
                        "Customer Lane 5"),
                    today.AddDays(-2)));
            issuedRepair = await warranties.IssueRepairWarrantyAsync(
                new IssueRepairWarrantyRequest(
                    product.Id,
                    repairPlan.Id,
                    null,
                    new CustomerInput("Grace Hopper", "grace@example.test"),
                    today.AddDays(-1),
                    "Replaced the display and battery",
                    [
                        new ReplacedPartInput("Display module", "DSP-100"),
                        new ReplacedPartInput("Battery", "BAT-200")
                    ]));

            await categories.UpdateAsync(
                category.Id,
                new UpdateCategoryRequest("Current phones", "Changed after issuance"));
            await products.UpdateAsync(
                product.Id,
                new UpdateProductRequest(
                    category.Id,
                    "Current phone",
                    999m,
                    "EUR",
                    "Changed after issuance"));
            await issuer.SaveAsync(new SaveIssuerProfileRequest(
                "Current issuer",
                "BE9999999999",
                "Current Warranty Street 99"));
        }

        await using (var scope = database.Services.CreateAsyncScope())
        {
            var products = scope.ServiceProvider.GetRequiredService<ProductQueryService>();
            var warranties = scope.ServiceProvider.GetRequiredService<WarrantyQueryService>();

            var currentProduct = await products.GetAsync(productId);
            Assert.NotNull(currentProduct);
            Assert.Equal("Current phone", currentProduct.Name);
            Assert.Equal(999m, currentProduct.PriceAmount);

            var storedSale = await warranties.GetAsync(issuedSale.Id);
            Assert.NotNull(storedSale);
            Assert.Equal(WarrantyType.ProductSale, storedSale.Type);
            Assert.Equal("Phone at issuance", storedSale.Product.ProductName);
            Assert.Equal("Phones at issuance", storedSale.Product.CategoryName);
            Assert.Equal(1234.5678m, storedSale.Product.PriceAmount);
            Assert.Equal("EUR", storedSale.Product.PriceCurrencyCode);
            Assert.Equal("Issuer at issuance", storedSale.Issuer.Name);
            Assert.Equal("Original Warranty Street 10", storedSale.Issuer.Address);
            Assert.Equal("Ada Lovelace", storedSale.Customer.FullName);
            Assert.Equal(issuedSale.StartsOn, storedSale.StartsOn);
            Assert.Equal(issuedSale.ExpiresOn, storedSale.ExpiresOn);
            Assert.NotNull(storedSale.SaleDetails);
            Assert.Null(storedSale.RepairDetails);

            var storedRepair = await warranties.GetByCertificateNumberAsync(
                issuedRepair.CertificateNumber.ToLowerInvariant());
            Assert.NotNull(storedRepair);
            Assert.Equal(WarrantyType.Repair, storedRepair.Type);
            Assert.Equal("Phone at issuance", storedRepair.Product.ProductName);
            Assert.Equal("Phones at issuance", storedRepair.Product.CategoryName);
            Assert.Equal(1234.5678m, storedRepair.Product.PriceAmount);
            Assert.Equal("Issuer at issuance", storedRepair.Issuer.Name);
            Assert.Equal("Replaced the display and battery", storedRepair.RepairDetails!.WorkDescription);
            Assert.Equal(
                ["Display module", "Battery"],
                storedRepair.RepairDetails.ReplacedParts.Select(part => part.Description));
            Assert.Equal(
                ["DSP-100", "BAT-200"],
                storedRepair.RepairDetails.ReplacedParts.Select(part => part.PartNumber));
            Assert.Null(storedRepair.SaleDetails);
            Assert.Equal(2, (await warranties.ListAsync()).Count);
        }
    }

    [Fact]
    public async Task Category_archival_requires_active_products_to_be_reassigned_first()
    {
        await using var database = await MigratedDatabase.CreateAsync();

        await using var scope = database.Services.CreateAsyncScope();
        var categoryCommands = scope.ServiceProvider.GetRequiredService<CategoryCommandService>();
        var categoryQueries = scope.ServiceProvider.GetRequiredService<CategoryQueryService>();
        var productCommands = scope.ServiceProvider.GetRequiredService<ProductCommandService>();
        var productQueries = scope.ServiceProvider.GetRequiredService<ProductQueryService>();
        var issuer = scope.ServiceProvider.GetRequiredService<IssuerProfileService>();
        var warrantyCommands = scope.ServiceProvider.GetRequiredService<WarrantyCommandService>();
        var warrantyQueries = scope.ServiceProvider.GetRequiredService<WarrantyQueryService>();
        var planCommands = scope.ServiceProvider.GetRequiredService<WarrantyPlanCommandService>();

        var unused = await categoryCommands.CreateAsync(new CreateCategoryRequest("Unused"));
        await categoryCommands.ArchiveAsync(unused.Id);
        Assert.Null(await categoryQueries.GetAsync(unused.Id));

        var originalCategory = await categoryCommands.CreateAsync(new CreateCategoryRequest("Phones"));
        var replacementCategory = await categoryCommands.CreateAsync(new CreateCategoryRequest("Devices"));
        var product = await productCommands.CreateAsync(new CreateProductRequest(
            originalCategory.Id,
            "Reassignable phone",
            800m,
            "EUR"));
        await issuer.SaveAsync(new SaveIssuerProfileRequest(
            "Warranty Workshop",
            null,
            "Warranty Street 10"));
        var salePlan = await planCommands.CreateAsync(new CreateWarrantyPlanRequest(
            WarrantyType.ProductSale,
            "Standard product sale",
            12,
            "Product coverage"));
        var repairPlan = await planCommands.CreateAsync(new CreateWarrantyPlanRequest(
            WarrantyType.Repair,
            "Standard repair",
            3,
            "Repair coverage"));

        var exception = await Assert.ThrowsAsync<CategoryInUseException>(
            () => categoryCommands.ArchiveAsync(originalCategory.Id));
        Assert.Equal(originalCategory.Id, exception.CategoryId);
        Assert.NotNull(await categoryQueries.GetAsync(originalCategory.Id));
        Assert.Equal(originalCategory.Id, (await productQueries.GetAsync(product.Id))!.CategoryId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sale = await warrantyCommands.IssueProductSaleWarrantyAsync(
            new IssueProductSaleWarrantyRequest(
                product.Id,
                salePlan.Id,
                null,
                new CustomerInput("Existing customer"),
                today.AddDays(-2)));

        await productCommands.UpdateAsync(
            product.Id,
            new UpdateProductRequest(
                replacementCategory.Id,
                product.Name,
                product.PriceAmount,
                product.CurrencyCode,
                product.Description));
        await categoryCommands.ArchiveAsync(originalCategory.Id);

        Assert.Null(await categoryQueries.GetAsync(originalCategory.Id));
        var reassignedProduct = await productQueries.GetAsync(product.Id);
        Assert.NotNull(reassignedProduct);
        Assert.Equal(replacementCategory.Id, reassignedProduct.CategoryId);

        var repair = await warrantyCommands.IssueRepairWarrantyAsync(
            new IssueRepairWarrantyRequest(
                product.Id,
                repairPlan.Id,
                null,
                new CustomerInput("Existing customer"),
                today.AddDays(-1),
                "Labor-only repair"));

        var storedSale = await warrantyQueries.GetAsync(sale.Id);
        var storedRepair = await warrantyQueries.GetAsync(repair.Id);
        Assert.Equal("Phones", storedSale!.Product.CategoryName);
        Assert.Equal(originalCategory.Id, storedSale.Product.CategoryId);
        Assert.Equal("Devices", storedRepair!.Product.CategoryName);
        Assert.Equal(replacementCategory.Id, storedRepair.Product.CategoryId);
    }

    private sealed class MigratedDatabase : IAsyncDisposable
    {
        private MigratedDatabase(string path, ServiceProvider services)
        {
            Path = path;
            Services = services;
        }

        private string Path { get; }

        public ServiceProvider Services { get; }

        public static async Task<MigratedDatabase> CreateAsync()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"WarrantyIssueXE-application-{Guid.NewGuid():N}.db");
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInventoryInfrastructure(path, TimeZoneInfo.Utc);
            serviceCollection.AddScoped<CategoryCommandService>();
            serviceCollection.AddScoped<CategoryQueryService>();
            serviceCollection.AddScoped<ProductCommandService>();
            serviceCollection.AddScoped<ProductQueryService>();
            serviceCollection.AddScoped<IssuerProfileService>();
            serviceCollection.AddScoped<WarrantyCommandService>();
            serviceCollection.AddScoped<WarrantyQueryService>();
            serviceCollection.AddScoped<WarrantyPlanCommandService>();
            var services = serviceCollection.BuildServiceProvider(
                new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

            await using (var scope = services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                var knownMigrations = context.Database.GetMigrations().ToArray();
                Assert.Single(knownMigrations);
                Assert.EndsWith("_InitialCreate", knownMigrations[0], StringComparison.Ordinal);

                await context.Database.MigrateAsync();

                Assert.Equal(knownMigrations, await context.Database.GetAppliedMigrationsAsync());
                Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            }

            return new MigratedDatabase(path, services);
        }

        public async ValueTask DisposeAsync()
        {
            await Services.DisposeAsync();
            DeleteIfPresent(Path);
            DeleteIfPresent($"{Path}-shm");
            DeleteIfPresent($"{Path}-wal");
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
