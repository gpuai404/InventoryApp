using WarrantyIssueXE.Application.Abstractions.Certificates;
using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.Abstractions.Time;
using WarrantyIssueXE.Application.Common;
using WarrantyIssueXE.Application.Warranties;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;
using DomainIssuerProfile = WarrantyIssueXE.Domain.ValueObjects.IssuerProfile;

namespace WarrantyIssueXE.Application.Tests;

public sealed class WarrantyCommandServiceTests
{
    [Fact]
    public async Task Issuance_uses_current_issuer_and_does_not_require_a_company_id()
    {
        var fixture = new Fixture(new DomainIssuerProfile("Contoso Retail", "BE 0123"));
        var issued = await fixture.Service.IssueForSaleAsync(new IssueWarrantyForSaleRequest(
            fixture.Variant.Id.Value, null, new CoverageInput(24, "Parts and labour"), "SN-001",
            new CustomerInput("Ada"), new DateOnly(2026, 1, 1), 100m, "EUR"));

        Assert.Equal("Contoso Retail", issued.Issuer.Name);
        Assert.Equal("BE 0123", issued.Issuer.VatNumber);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Missing_issuer_profile_blocks_issuance_without_saving()
    {
        var fixture = new Fixture(null);
        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.Service.IssueForSaleAsync(new IssueWarrantyForSaleRequest(
            fixture.Variant.Id.Value, null, new CoverageInput(12, "Coverage"), null,
            new CustomerInput("Ada"), new DateOnly(2026, 1, 1), 1m, "EUR")));
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Editing_the_profile_does_not_rewrite_an_existing_warranty_snapshot()
    {
        var fixture = new Fixture(new DomainIssuerProfile("Original issuer", "BE-OLD"));
        var first = await fixture.Service.IssueForSaleAsync(new IssueWarrantyForSaleRequest(
            fixture.Variant.Id.Value, null, new CoverageInput(12, "Coverage"), null,
            new CustomerInput("Ada"), new DateOnly(2026, 1, 1), 1m, "EUR"));

        fixture.IssuerStore.Profile = new DomainIssuerProfile("Updated issuer", "BE-NEW");
        var second = await fixture.Service.IssueForSaleAsync(new IssueWarrantyForSaleRequest(
            fixture.Variant.Id.Value, null, new CoverageInput(12, "Coverage"), null,
            new CustomerInput("Ada"), new DateOnly(2026, 1, 1), 1m, "EUR"));

        Assert.Equal("Original issuer", first.Issuer.Name);
        Assert.Equal("BE-OLD", fixture.Warranties.Items[0].Issuer.VatNumber);
        Assert.Equal("Updated issuer", second.Issuer.Name);
    }

    [Fact]
    public async Task Warranty_plan_must_match_the_requested_warranty_type()
    {
        var fixture = new Fixture(new DomainIssuerProfile("Issuer"));
        var repairPlan = new WarrantyPlan(
            WarrantyType.Repair,
            "Repair plan",
            new WarrantyTerm(6),
            "Repair coverage");
        fixture.Plans.Items[repairPlan.Id] = repairPlan;

        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            fixture.Service.IssueForSaleAsync(new IssueWarrantyForSaleRequest(
                fixture.Variant.Id.Value, repairPlan.Id.Value, null, null,
                new CustomerInput("Ada"), new DateOnly(2026, 1, 1), 1m, "EUR")));

        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    private sealed class Fixture
    {
        public Fixture(DomainIssuerProfile? issuer)
        {
            var product = new Product(CategoryId.New(), "Laptop", new Money(1m, "EUR"));
            Variant = new ProductVariant(product.Id, "Black");
            Products.Items[product.Id] = product;
            Variants.Items[Variant.Id] = Variant;
            IssuerStore = new FakeIssuerStore(issuer);
            Service = new WarrantyCommandService(
                Variants, Products, IssuerStore, Plans, Warranties, Warranties, UnitOfWork,
                new FixedNumberGenerator(), new FixedClock());
        }

        public ProductVariant Variant { get; }
        public FakeProductReader Products { get; } = new();
        public FakeVariantReader Variants { get; } = new();
        public FakePlanReader Plans { get; } = new();
        public FakeWarrantyRepository Warranties { get; } = new();
        public FakeIssuerStore IssuerStore { get; }
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public WarrantyCommandService Service { get; }
    }

    private sealed class FakeIssuerStore(DomainIssuerProfile? profile) : IIssuerProfileStore
    {
        public DomainIssuerProfile? Profile { get; set; } = profile;
        public Task<DomainIssuerProfile?> GetCurrentAsync(CancellationToken cancellationToken = default) => Task.FromResult(Profile);
        public Task SaveAsync(DomainIssuerProfile issuerProfile, CancellationToken cancellationToken = default)
        {
            Profile = issuerProfile;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProductReader : IProductReader
    {
        public Dictionary<ProductId, Product> Items { get; } = [];
        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default) => Task.FromResult(Items.GetValueOrDefault(id));
        public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Product>>(Items.Values.ToArray());
    }

    private sealed class FakeVariantReader : IProductVariantReader
    {
        public Dictionary<ProductVariantId, ProductVariant> Items { get; } = [];
        public Task<ProductVariant?> GetByIdAsync(ProductVariantId id, CancellationToken cancellationToken = default) => Task.FromResult(Items.GetValueOrDefault(id));
        public Task<IReadOnlyList<ProductVariant>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ProductVariant>>(Items.Values.ToArray());
        public Task<IReadOnlyList<ProductVariant>> ListByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ProductVariant>>(Items.Values.Where(item => item.ProductId == productId).ToArray());
        public Task<ProductVariant?> FindByBarcodeAsync(ProductBarcode barcode, CancellationToken cancellationToken = default) => Task.FromResult<ProductVariant?>(null);
        public Task<IReadOnlySet<string>> FindReservedBarcodeCodesAsync(IReadOnlyCollection<string> barcodeCodes, ProductVariantId? excludingVariantId = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
    }

    private sealed class FakePlanReader : IWarrantyPlanReader
    {
        public Dictionary<WarrantyPlanId, WarrantyPlan> Items { get; } = [];
        public Task<WarrantyPlan?> GetByIdAsync(WarrantyPlanId id, CancellationToken cancellationToken = default) => Task.FromResult(Items.GetValueOrDefault(id));
        public Task<IReadOnlyList<WarrantyPlan>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<WarrantyPlan>>(Items.Values.ToArray());
    }

    private sealed class FakeWarrantyRepository : IWarrantyReader, IWarrantyWriter
    {
        public List<Warranty> Items { get; } = [];
        public Task<Warranty?> GetByIdAsync(WarrantyId id, CancellationToken cancellationToken = default) => Task.FromResult<Warranty?>(null);
        public Task<Warranty?> GetByCertificateNumberAsync(string certificateNumber, CancellationToken cancellationToken = default) => Task.FromResult<Warranty?>(null);
        public Task<IReadOnlyList<Warranty>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Warranty>>([]);
        public Task AddAsync(Warranty warranty, CancellationToken cancellationToken = default)
        {
            Items.Add(warranty);
            return Task.CompletedTask;
        }
        public Task VoidAsync(Warranty voidedWarranty, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) { SaveCount++; return Task.CompletedTask; }
    }

    private sealed class FixedNumberGenerator : ICertificateNumberGenerator { public string Generate() => "WR-001"; }
    private sealed class FixedClock : IBusinessClock
    {
        public BusinessTime GetCurrent() => new(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), new DateOnly(2026, 1, 2));
        public DateTimeOffset GetUtcNow() => GetCurrent().UtcNow;
        public DateOnly GetBusinessDate() => GetCurrent().BusinessDate;
    }
}
