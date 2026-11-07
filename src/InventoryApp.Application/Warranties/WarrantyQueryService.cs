using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Application.Warranties;

public sealed class WarrantyQueryService
{
    private readonly IWarrantyReader _reader;

    public WarrantyQueryService(IWarrantyReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<WarrantyDto?> GetAsync(
        Guid warrantyId,
        CancellationToken cancellationToken = default)
    {
        var warranty = await _reader.GetByIdAsync(
            new WarrantyId(warrantyId),
            cancellationToken);

        return warranty?.ToDto();
    }

    public async Task<IReadOnlyList<WarrantyDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var warranties = await _reader.ListAsync(cancellationToken);
        return warranties.Select(warranty => warranty.ToDto()).ToArray();
    }
}
