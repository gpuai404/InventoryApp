using WarrantyIssueXE.Application.Abstractions.Certificates;
using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.Abstractions.Time;
using WarrantyIssueXE.Application.Common;
using WarrantyIssueXE.Application.Warranties;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.ValueObjects;
using DomainIssuerProfile = WarrantyIssueXE.Domain.ValueObjects.IssuerProfile;

namespace WarrantyIssueXE.Application.Tests;

public sealed class WarrantyCommandServiceTests
{
    [Fact]
    public async Task Product_sale_issuance_loads_product_and_captures_price_category_customer_and_issuer()
    {
        var fixture = new Fixture();
        var plan = fixture.AddPlan(WarrantyType.ProductSale, 24, "Parts and labour");

        var issued = await fixture.Service.IssueProductSaleWarrantyAsync(
            fixture.SaleRequest(plan.Id.Value));

        Assert.Equal(WarrantyType.ProductSale, issued.Type);
        Assert.Equal(plan.Id.Value, issued.WarrantyPlanId);
        Assert.Equal("490154203237518", issued.Imei);
        Assert.Equal(fixture.Product.Id.Value, issued.Product.ProductId);
        Assert.Equal(fixture.Category.Id.Value, issued.Product.CategoryId);
        Assert.Equal(fixture.Category.Name, issued.Product.CategoryName);
        Assert.Equal(899.99m, issued.Product.PriceAmount);
        Assert.Equal("EUR", issued.Product.PriceCurrencyCode);
        Assert.Equal("Ada", issued.Customer.FullName);
        Assert.Equal("Contoso Retail", issued.Issuer.Name);
        Assert.Equal("Avenue des Arts 1", issued.Issuer.Address);
        Assert.Equal(24, issued.Coverage.TermMonths);
        Assert.NotNull(issued.SaleDetails);
        Assert.Null(issued.RepairDetails);
        Assert.Equal(fixture.Product.Id, fixture.Products.RequestedIds.Single());
        Assert.Single(fixture.Warranties.Items);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Product_sale_rejects_a_repair_plan_without_persisting()
    {
        var fixture = new Fixture();
        var repairPlan = fixture.AddPlan(WarrantyType.Repair, 6, "Repair work");

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            fixture.Service.IssueProductSaleWarrantyAsync(
                fixture.SaleRequest(repairPlan.Id.Value)));

        Assert.Equal(DomainErrorCodes.WarrantyPlanTypeMismatch, exception.Code);
        Assert.Empty(fixture.Warranties.Items);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Repair_issuance_uses_repair_plan_and_supports_labor_only_work()
    {
        var fixture = new Fixture();
        var plan = fixture.AddPlan(WarrantyType.Repair, 6, "Repair work and replaced parts");

        var issued = await fixture.Service.IssueRepairWarrantyAsync(
            fixture.RepairRequest(plan.Id.Value));

        Assert.Equal(WarrantyType.Repair, issued.Type);
        Assert.Equal(plan.Id.Value, issued.WarrantyPlanId);
        Assert.Equal(899.99m, issued.Product.PriceAmount);
        Assert.Equal(6, issued.Coverage.TermMonths);
        Assert.Equal(CoverageScope.RepairWorkAndReplacedParts, issued.Coverage.Scope);
        Assert.Equal(CoverageSnapshot.RepairScopeStatement, issued.Coverage.ScopeStatement);
        Assert.Empty(issued.RepairDetails!.ReplacedParts);
        Assert.Null(issued.SaleDetails);
    }

    [Fact]
    public async Task Repair_issuance_preserves_replaced_part_descriptions()
    {
        var fixture = new Fixture();
        var plan = fixture.AddPlan(WarrantyType.Repair, 6, "Repair work");
        var request = fixture.RepairRequest(
            plan.Id.Value,
            [new ReplacedPartInput("Battery", "BT-42"), new ReplacedPartInput("Connector")]);

        var issued = await fixture.Service.IssueRepairWarrantyAsync(request);

        Assert.Collection(
            issued.RepairDetails!.ReplacedParts,
            part =>
            {
                Assert.Equal("Battery", part.Description);
                Assert.Equal("BT-42", part.PartNumber);
            },
            part => Assert.Equal("Connector", part.Description));
    }

    [Fact]
    public async Task Repair_rejects_a_product_sale_plan_without_persisting()
    {
        var fixture = new Fixture();
        var salePlan = fixture.AddPlan(WarrantyType.ProductSale, 24, "Product coverage");

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            fixture.Service.IssueRepairWarrantyAsync(
                fixture.RepairRequest(salePlan.Id.Value)));

        Assert.Equal(DomainErrorCodes.WarrantyPlanTypeMismatch, exception.Code);
        Assert.Empty(fixture.Warranties.Items);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Missing_product_blocks_issuance()
    {
        var fixture = new Fixture();
        fixture.Products.Items.Clear();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            fixture.Service.IssueProductSaleWarrantyAsync(
                fixture.SaleRequest()));

        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Missing_plan_blocks_issuance()
    {
        var fixture = new Fixture();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            fixture.Service.IssueProductSaleWarrantyAsync(
                fixture.SaleRequest(Guid.NewGuid())));

        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Missing_issuer_configuration_blocks_issuance()
    {
        var fixture = new Fixture(useDefaultIssuer: false);

        var exception = await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            fixture.Service.IssueProductSaleWarrantyAsync(
                fixture.SaleRequest()));

        Assert.Equal("issuerProfile", exception.ParamName);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Invalid_customer_input_propagates_domain_validation_without_persisting()
    {
        var fixture = new Fixture();
        var request = fixture.SaleRequest() with
        {
            Customer = new CustomerInput(" ")
        };

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            fixture.Service.IssueProductSaleWarrantyAsync(request));

        Assert.Empty(fixture.Warranties.Items);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Invalid_repair_description_propagates_domain_validation_without_persisting()
    {
        var fixture = new Fixture();
        var request = fixture.RepairRequest() with
        {
            WorkDescription = " "
        };

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            fixture.Service.IssueRepairWarrantyAsync(request));

        Assert.Empty(fixture.Warranties.Items);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Invalid_imei_propagates_domain_validation_without_persisting()
    {
        var fixture = new Fixture();
        var request = fixture.SaleRequest() with
        {
            Imei = "invalid"
        };

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            fixture.Service.IssueProductSaleWarrantyAsync(request));

        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Persistence_failure_is_propagated_without_committing()
    {
        var fixture = new Fixture();
        fixture.Warranties.AddException = new InvalidOperationException("Database unavailable");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.IssueProductSaleWarrantyAsync(
                fixture.SaleRequest()));

        Assert.Equal("Database unavailable", exception.Message);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    private sealed class Fixture
    {
        public Fixture(DomainIssuerProfile? issuer = null, bool useDefaultIssuer = true)
        {
            Category = new Category("Computers");
            Product = new Product(Category.Id, "Laptop", new Money(899.99m, "EUR"));
            Products.Items[Product.Id] = Product;
            Categories.Items[Category.Id] = Category;
            SalePlan = AddPlan(WarrantyType.ProductSale, 12, "Standard product coverage");
            RepairPlan = AddPlan(WarrantyType.Repair, 6, "Repair coverage");
            IssuerStore.Profile = useDefaultIssuer
                ? issuer ?? new DomainIssuerProfile(
                    "Contoso Retail",
                    "BE 0123",
                    "Avenue des Arts 1",
                    "contact@example.test",
                    "+32 2 555 0100")
                : issuer;

            Service = new WarrantyCommandService(
                Products,
                Categories,
                IssuerStore,
                Plans,
                Warranties,
                UnitOfWork,
                new FixedNumberGenerator(),
                new FixedClock());
        }

        public Product Product { get; }
        public Category Category { get; }
        public WarrantyPlan SalePlan { get; }
        public WarrantyPlan RepairPlan { get; }
        public FakeProductReader Products { get; } = new();
        public FakeCategoryReader Categories { get; } = new();
        public FakePlanReader Plans { get; } = new();
        public FakeWarrantyWriter Warranties { get; } = new();
        public FakeIssuerStore IssuerStore { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public WarrantyCommandService Service { get; }

        public WarrantyPlan AddPlan(WarrantyType type, int months, string description)
        {
            var plan = new WarrantyPlan(type, $"{type} plan", new WarrantyTerm(months), description);
            Plans.Items[plan.Id] = plan;
            return plan;
        }

        public IssueProductSaleWarrantyRequest SaleRequest(Guid? warrantyPlanId = null) =>
            new(
                Product.Id.Value,
                warrantyPlanId ?? SalePlan.Id.Value,
                "490154203237518",
                new CustomerInput(
                    "Ada",
                    "ada@example.test",
                    "+32 2 555 0200",
                    "Customer Street 1"),
                new DateOnly(2026, 1, 1));

        public IssueRepairWarrantyRequest RepairRequest(
            Guid? warrantyPlanId = null,
            IReadOnlyCollection<ReplacedPartInput>? replacedParts = null) =>
            new(
                Product.Id.Value,
                warrantyPlanId ?? RepairPlan.Id.Value,
                null,
                new CustomerInput("Ada"),
                new DateOnly(2026, 1, 1),
                "Repaired charging connector",
                replacedParts);
    }

    private sealed class FakeProductReader : IProductReader
    {
        public Dictionary<ProductId, Product> Items { get; } = [];
        public List<ProductId> RequestedIds { get; } = [];

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default)
        {
            RequestedIds.Add(id);
            return Task.FromResult(Items.GetValueOrDefault(id));
        }

        public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Product>>(Items.Values.ToArray());

        public Task<bool> HasActiveProductsInCategoryAsync(
            CategoryId categoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Values.Any(product => product.CategoryId == categoryId));
    }

    private sealed class FakeCategoryReader : ICategoryReader
    {
        public Dictionary<CategoryId, Category> Items { get; } = [];

        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(id));

        public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>(Items.Values.ToArray());
    }

    private sealed class FakeIssuerStore : IIssuerProfileStore
    {
        public DomainIssuerProfile? Profile { get; set; }

        public Task<DomainIssuerProfile?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Profile);

        public Task SaveAsync(
            DomainIssuerProfile profile,
            CancellationToken cancellationToken = default)
        {
            Profile = profile;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlanReader : IWarrantyPlanReader
    {
        public Dictionary<WarrantyPlanId, WarrantyPlan> Items { get; } = [];

        public Task<WarrantyPlan?> GetByIdAsync(
            WarrantyPlanId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(id));

        public Task<IReadOnlyList<WarrantyPlan>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WarrantyPlan>>(Items.Values.ToArray());
    }

    private sealed class FakeWarrantyWriter : IWarrantyWriter
    {
        public List<Warranty> Items { get; } = [];
        public Exception? AddException { get; set; }

        public Task AddAsync(Warranty warranty, CancellationToken cancellationToken = default)
        {
            if (AddException is not null)
            {
                return Task.FromException(AddException);
            }

            Items.Add(warranty);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedNumberGenerator : ICertificateNumberGenerator
    {
        public string Generate() => "WR-001";
    }

    private sealed class FixedClock : IBusinessClock
    {
        public BusinessTime GetCurrent() =>
            new(
                new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero),
                new DateOnly(2026, 1, 2));

        public DateTimeOffset GetUtcNow() => GetCurrent().UtcNow;

        public DateOnly GetBusinessDate() => GetCurrent().BusinessDate;
    }
}
