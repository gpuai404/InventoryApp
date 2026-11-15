using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Infrastructure.Persistence;
using WarrantyIssueXE.Infrastructure.Persistence.Mappings;
using Microsoft.EntityFrameworkCore;

namespace WarrantyIssueXE.Infrastructure.Persistence.Repositories;

public sealed class IssuerProfileRepository(InventoryDbContext dbContext) : IIssuerProfileStore
{
    private readonly InventoryDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<IssuerProfile?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        (await _dbContext.IssuerProfiles.AsNoTracking().SingleOrDefaultAsync(
            profile => profile.Id == IssuerProfileRecord.SingletonId,
            cancellationToken))?.ToDomain();

    public async Task SaveAsync(IssuerProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var record = await _dbContext.IssuerProfiles.SingleOrDefaultAsync(
            current => current.Id == IssuerProfileRecord.SingletonId,
            cancellationToken);

        if (record is null)
        {
            _dbContext.IssuerProfiles.Add(profile.ToRecord());
            return;
        }

        record.Name = profile.Name;
        record.VatNumber = profile.VatNumber;
        record.Address = profile.Address;
        record.Email = profile.Email;
        record.Phone = profile.Phone;
    }
}
