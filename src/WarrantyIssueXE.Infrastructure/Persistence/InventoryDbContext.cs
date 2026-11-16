using Microsoft.EntityFrameworkCore;

namespace WarrantyIssueXE.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    internal DbSet<CategoryRecord> Categories => Set<CategoryRecord>();
    internal DbSet<IssuerProfileRecord> IssuerProfiles => Set<IssuerProfileRecord>();
    internal DbSet<ProductRecord> Products => Set<ProductRecord>();
    internal DbSet<WarrantyPlanRecord> WarrantyPlans => Set<WarrantyPlanRecord>();
    internal DbSet<WarrantyRecord> Warranties => Set<WarrantyRecord>();
    internal DbSet<WarrantyReplacedPartRecord> WarrantyReplacedParts => Set<WarrantyReplacedPartRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
}
