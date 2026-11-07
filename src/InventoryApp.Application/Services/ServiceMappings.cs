using InventoryApp.Domain.Entities;

namespace InventoryApp.Application.Services;

internal static class ServiceMappings
{
    public static ServiceDto ToDto(this Service service) =>
        new(
            service.Id.Value,
            service.CompanyId.Value,
            service.Name,
            service.WarrantyTerm.Months,
            service.Description);
}
