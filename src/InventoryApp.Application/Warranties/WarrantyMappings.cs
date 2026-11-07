using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Warranties;

internal static class WarrantyMappings
{
    public static WarrantyDto ToDto(this Warranty warranty) =>
        new(
            warranty.Id.Value,
            warranty.ProductId.Value,
            warranty.ServiceId.Value,
            warranty.Description,
            warranty.StartsOn,
            warranty.ExpiresOn);
}
