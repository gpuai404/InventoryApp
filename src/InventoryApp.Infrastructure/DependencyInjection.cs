using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        string sqliteDatabasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqliteDatabasePath);

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlite($"Data Source={sqliteDatabasePath}"));
        services.AddSingleton<TimeProvider>(TimeProvider.System);

        services.AddScoped<EfUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider =>
            provider.GetRequiredService<EfUnitOfWork>());

        services.AddScoped<CategoryRepository>();
        services.AddScoped<ICategoryReader>(provider =>
            provider.GetRequiredService<CategoryRepository>());
        services.AddScoped<ICategoryWriter>(provider =>
            provider.GetRequiredService<CategoryRepository>());

        services.AddScoped<CompanyRepository>();
        services.AddScoped<ICompanyReader>(provider =>
            provider.GetRequiredService<CompanyRepository>());
        services.AddScoped<ICompanyWriter>(provider =>
            provider.GetRequiredService<CompanyRepository>());

        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductReader>(provider =>
            provider.GetRequiredService<ProductRepository>());
        services.AddScoped<IProductWriter>(provider =>
            provider.GetRequiredService<ProductRepository>());

        services.AddScoped<ProductVariantRepository>();
        services.AddScoped<IProductVariantReader>(provider =>
            provider.GetRequiredService<ProductVariantRepository>());
        services.AddScoped<IProductVariantWriter>(provider =>
            provider.GetRequiredService<ProductVariantRepository>());

        services.AddScoped<ServiceRepository>();
        services.AddScoped<IServiceReader>(provider =>
            provider.GetRequiredService<ServiceRepository>());
        services.AddScoped<IServiceWriter>(provider =>
            provider.GetRequiredService<ServiceRepository>());

        services.AddScoped<WarrantyRepository>();
        services.AddScoped<IWarrantyReader>(provider =>
            provider.GetRequiredService<WarrantyRepository>());
        services.AddScoped<IWarrantyWriter>(provider =>
            provider.GetRequiredService<WarrantyRepository>());

        return services;
    }
}
