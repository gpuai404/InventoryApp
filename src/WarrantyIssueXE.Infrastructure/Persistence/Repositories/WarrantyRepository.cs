using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Infrastructure.Persistence;
using WarrantyIssueXE.Infrastructure.Persistence.Mappings;
using Microsoft.EntityFrameworkCore;

namespace WarrantyIssueXE.Infrastructure.Persistence.Repositories;

public sealed class WarrantyRepository(InventoryDbContext dbContext) : IWarrantyReader, IWarrantyWriter
{
    private readonly InventoryDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<Warranty?> GetByIdAsync(WarrantyId id, CancellationToken cancellationToken = default) =>
        (await QueryWithParts().AsNoTracking().SingleOrDefaultAsync(
            warranty => warranty.Id == id.Value,
            cancellationToken))?.ToDomain();

    public async Task<Warranty?> GetByCertificateNumberAsync(
        string certificateNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certificateNumber);
        var normalizedCertificateNumber = Warranty.NormalizeCertificateNumber(certificateNumber);

        return (await QueryWithParts().AsNoTracking().SingleOrDefaultAsync(
            warranty => warranty.CertificateNumber == normalizedCertificateNumber,
            cancellationToken))?.ToDomain();
    }

    public async Task<IReadOnlyList<Warranty>> ListAsync(CancellationToken cancellationToken = default) =>
        (await QueryWithParts().AsNoTracking()
            .OrderByDescending(warranty => warranty.IssuedAtUtcTicks)
            .ThenBy(warranty => warranty.CertificateNumber)
            .ToListAsync(cancellationToken))
        .Select(warranty => warranty.ToDomain())
        .ToArray();

    public Task AddAsync(Warranty warranty, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(warranty);
        _dbContext.Warranties.Add(warranty.ToRecord());
        return Task.CompletedTask;
    }

    private IQueryable<WarrantyRecord> QueryWithParts() =>
        _dbContext.Warranties.Include(warranty => warranty.ReplacedParts);
}
