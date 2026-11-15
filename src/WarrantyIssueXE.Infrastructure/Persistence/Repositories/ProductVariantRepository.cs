using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Infrastructure.Persistence;
using WarrantyIssueXE.Infrastructure.Persistence.Mappings;
using Microsoft.EntityFrameworkCore;

namespace WarrantyIssueXE.Infrastructure.Persistence.Repositories;

public sealed class ProductVariantRepository(InventoryDbContext dbContext, TimeProvider timeProvider)
    : IProductVariantReader, IProductVariantWriter
{
    private readonly InventoryDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<ProductVariant?> GetByIdAsync(
        ProductVariantId id,
        CancellationToken cancellationToken = default) =>
        (await QueryWithBarcodes().AsNoTracking().SingleOrDefaultAsync(
            variant => variant.Id == id.Value,
            cancellationToken))?.ToDomain();

    public async Task<IReadOnlyList<ProductVariant>> ListAsync(CancellationToken cancellationToken = default) =>
        (await QueryWithBarcodes().AsNoTracking()
            .OrderBy(variant => variant.Name)
            .ToListAsync(cancellationToken))
        .Select(variant => variant.ToDomain())
        .ToArray();

    public async Task<IReadOnlyList<ProductVariant>> ListByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default) =>
        (await QueryWithBarcodes().AsNoTracking()
            .Where(variant => variant.ProductId == productId.Value)
            .OrderBy(variant => variant.Name)
            .ToListAsync(cancellationToken))
        .Select(variant => variant.ToDomain())
        .ToArray();

    public async Task<ProductVariant?> FindByBarcodeAsync(
        ProductBarcode barcode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(barcode);

        return (await QueryWithBarcodes().AsNoTracking().SingleOrDefaultAsync(
            variant => variant.Barcodes.Any(candidate => candidate.Code == barcode.Code),
            cancellationToken))?.ToDomain();
    }

    public async Task<IReadOnlySet<string>> FindReservedBarcodeCodesAsync(
        IReadOnlyCollection<string> barcodeCodes,
        ProductVariantId? excludingVariantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(barcodeCodes);

        var codes = barcodeCodes.Distinct(StringComparer.Ordinal).ToArray();
        if (codes.Length == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var query = _dbContext.Set<ProductBarcodeRecord>().Where(barcode => codes.Contains(barcode.Code));
        if (excludingVariantId is { } variantId)
        {
            query = query.Where(barcode => barcode.ProductVariantId != variantId.Value);
        }

        return (await query
            .Select(barcode => barcode.Code)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
    }

    public Task AddAsync(ProductVariant entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbContext.ProductVariants.Add(entity.ToRecord());
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(ProductVariant entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var record = await QueryWithBarcodes().SingleOrDefaultAsync(
            variant => variant.Id == entity.Id.Value,
            cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(ProductVariant), entity.Id.Value);

        // ProductId deliberately remains unchanged: a normal revision cannot move a variant.
        record.Name = entity.Name;
        record.Sku = entity.Sku;
        ReconcileBarcodes(record, entity);
        MarkForUpdate(record);
    }

    public async Task ArchiveAsync(ProductVariantId id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ProductVariants.SingleOrDefaultAsync(
            variant => variant.Id == id.Value,
            cancellationToken)
            ?? throw PersistenceErrors.MissingRecord(nameof(ProductVariant), id.Value);

        record.ArchivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
    }

    private IQueryable<ProductVariantRecord> QueryWithBarcodes() =>
        _dbContext.ProductVariants.Include(variant => variant.Barcodes);

    private void MarkForUpdate(ArchivableRecord record) =>
        _dbContext.Entry(record).Property(master => master.UpdatedAtUtc).IsModified = true;

    private static void ReconcileBarcodes(ProductVariantRecord record, ProductVariant variant)
    {
        var requestedBarcodes = variant.Barcodes.ToDictionary(barcode => barcode.Code, StringComparer.Ordinal);

        foreach (var existingBarcode in record.Barcodes
                     .Where(barcode => !requestedBarcodes.ContainsKey(barcode.Code))
                     .ToArray())
        {
            record.Barcodes.Remove(existingBarcode);
        }

        foreach (var existingBarcode in record.Barcodes)
        {
            existingBarcode.PackagingDescription = requestedBarcodes[existingBarcode.Code].PackagingDescription;
        }

        var existingCodes = record.Barcodes
            .Select(barcode => barcode.Code)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var barcode in variant.Barcodes.Where(barcode => !existingCodes.Contains(barcode.Code)))
        {
            record.Barcodes.Add(barcode.ToRecord(variant.Id));
        }
    }
}
