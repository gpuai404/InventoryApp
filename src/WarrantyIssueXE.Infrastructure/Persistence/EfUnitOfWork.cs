using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.IssuerProfiles;
using WarrantyIssueXE.Application.Products;
using WarrantyIssueXE.Application.Warranties;
using Microsoft.EntityFrameworkCore;

namespace WarrantyIssueXE.Infrastructure.Persistence;

public sealed class EfUnitOfWork(InventoryDbContext dbContext) : IUnitOfWork
{
    private readonly InventoryDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception) when (
            exception.Entries.SingleOrDefault(entry => entry.Entity is WarrantyRecord) is { Entity: WarrantyRecord record })
        {
            var exists = await _dbContext.Warranties
                .AsNoTracking()
                .AnyAsync(warranty => warranty.Id == record.Id, cancellationToken);
            if (!exists)
            {
                throw PersistenceErrors.MissingRecord("Warranty", record.Id);
            }

            throw new WarrantyLifecycleConflictException(record.Id);
        }
        catch (DbUpdateException exception) when (
            PersistenceErrors.GetDuplicateBarcodeCode(exception) is { } barcodeCode)
        {
            throw new ProductBarcodeConflictException(barcodeCode);
        }
        catch (DbUpdateException exception) when (
            PersistenceErrors.IsConcurrentIssuerProfileCreation(exception))
        {
            throw new IssuerProfileConflictException();
        }
    }
}
