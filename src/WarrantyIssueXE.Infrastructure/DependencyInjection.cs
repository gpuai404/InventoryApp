using WarrantyIssueXE.Application.Abstractions.Certificates;
using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.Abstractions.Time;
using WarrantyIssueXE.Infrastructure.Certificates;
using WarrantyIssueXE.Infrastructure.Persistence;
using WarrantyIssueXE.Infrastructure.Persistence.Repositories;
using WarrantyIssueXE.Infrastructure.Time;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WarrantyIssueXE.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers SQLite infrastructure for an operation-scoped DbContext. MAUI
    /// callers must create a DI scope for each UI operation rather than retaining
    /// a tracked context for the lifetime of the application.
    /// </summary>
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        string sqliteDatabasePath,
        TimeZoneInfo businessTimeZone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqliteDatabasePath);
        ArgumentNullException.ThrowIfNull(businessTimeZone);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = sqliteDatabasePath
        }.ToString();

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<MasterRecordAuditInterceptor>();
        services.AddDbContextFactory<InventoryDbContext>((serviceProvider, options) =>
            options
                .UseSqlite(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<MasterRecordAuditInterceptor>()));
        services.AddSingleton<IBusinessClock>(provider => new TimeZoneBusinessClock(
            provider.GetRequiredService<TimeProvider>(),
            businessTimeZone));
        services.AddSingleton<ICertificateNumberGenerator, GuidCertificateNumberGenerator>();

        services.AddScoped<EfUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<EfUnitOfWork>());

        services.AddScoped<CategoryRepository>();
        services.AddScoped<ICategoryReader>(provider => provider.GetRequiredService<CategoryRepository>());
        services.AddScoped<ICategoryWriter>(provider => provider.GetRequiredService<CategoryRepository>());

        services.AddScoped<IssuerProfileRepository>();
        services.AddScoped<IIssuerProfileStore>(provider => provider.GetRequiredService<IssuerProfileRepository>());

        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductReader>(provider => provider.GetRequiredService<ProductRepository>());
        services.AddScoped<IProductWriter>(provider => provider.GetRequiredService<ProductRepository>());

        services.AddScoped<ProductVariantRepository>();
        services.AddScoped<IProductVariantReader>(provider => provider.GetRequiredService<ProductVariantRepository>());
        services.AddScoped<IProductVariantWriter>(provider => provider.GetRequiredService<ProductVariantRepository>());

        services.AddScoped<WarrantyPlanRepository>();
        services.AddScoped<IWarrantyPlanReader>(provider => provider.GetRequiredService<WarrantyPlanRepository>());
        services.AddScoped<IWarrantyPlanWriter>(provider => provider.GetRequiredService<WarrantyPlanRepository>());

        services.AddScoped<WarrantyRepository>();
        services.AddScoped<IWarrantyReader>(provider => provider.GetRequiredService<WarrantyRepository>());
        services.AddScoped<IWarrantyWriter>(provider => provider.GetRequiredService<WarrantyRepository>());

        return services;
    }
}
