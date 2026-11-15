using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.IssuerProfiles;
using WarrantyIssueXE.Application.WarrantyPlans;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;
using DomainIssuerProfile = WarrantyIssueXE.Domain.ValueObjects.IssuerProfile;

namespace WarrantyIssueXE.Application.Tests;

public sealed class IssuerProfileAndWarrantyPlanCommandServiceTests
{
    [Fact]
    public async Task Issuer_profile_can_be_saved_and_replaced_as_one_singleton_value()
    {
        var store = new FakeIssuerProfileStore();
        var service = new IssuerProfileService(store, new FakeUnitOfWork());

        Assert.Null(await service.GetCurrentAsync());
        await service.SaveAsync(new SaveIssuerProfileRequest("Contoso", "BE 0123", "Street 1"));
        var current = await service.SaveAsync(new SaveIssuerProfileRequest("Contoso Retail", "BE 0456"));

        Assert.Equal("Contoso Retail", current.Name);
        Assert.Equal("BE 0456", (await store.GetCurrentAsync())!.VatNumber);
    }

    [Fact]
    public async Task Warranty_plan_is_created_without_company_ownership()
    {
        var plans = new FakeWarrantyPlanRepository();
        var service = new WarrantyPlanCommandService(plans, plans, new FakeUnitOfWork());
        var plan = await service.CreateAsync(new CreateWarrantyPlanRequest(
            WarrantyType.ProductSale, "Standard", 24, "Parts and labour"));

        Assert.Equal(WarrantyType.ProductSale, plan.Type);
        Assert.Equal(24, plan.TermMonths);
    }

    private sealed class FakeIssuerProfileStore : IIssuerProfileStore
    {
        private DomainIssuerProfile? _profile;
        public Task<DomainIssuerProfile?> GetCurrentAsync(CancellationToken cancellationToken = default) => Task.FromResult(_profile);
        public Task SaveAsync(DomainIssuerProfile profile, CancellationToken cancellationToken = default) { _profile = profile; return Task.CompletedTask; }
    }

    private sealed class FakeWarrantyPlanRepository : IWarrantyPlanReader, IWarrantyPlanWriter
    {
        private readonly Dictionary<WarrantyPlanId, WarrantyPlan> _items = [];
        public Task<WarrantyPlan?> GetByIdAsync(WarrantyPlanId id, CancellationToken cancellationToken = default) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<IReadOnlyList<WarrantyPlan>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<WarrantyPlan>>(_items.Values.ToArray());
        public Task AddAsync(WarrantyPlan entity, CancellationToken cancellationToken = default) { _items[entity.Id] = entity; return Task.CompletedTask; }
        public Task UpdateAsync(WarrantyPlan entity, CancellationToken cancellationToken = default) { _items[entity.Id] = entity; return Task.CompletedTask; }
        public Task ArchiveAsync(WarrantyPlanId id, CancellationToken cancellationToken = default) { _items.Remove(id); return Task.CompletedTask; }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
