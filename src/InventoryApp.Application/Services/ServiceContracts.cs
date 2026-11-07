namespace InventoryApp.Application.Services;

public sealed record CreateServiceRequest(
    Guid CompanyId,
    string Name,
    int WarrantyTermMonths,
    string? Description = null);

public sealed record UpdateServiceRequest(
    string Name,
    int WarrantyTermMonths,
    string? Description = null);

public sealed record ServiceDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    int WarrantyTermMonths,
    string? Description);
