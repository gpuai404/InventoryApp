namespace InventoryApp.Application.Warranties;

public sealed record CreateWarrantyRequest(
    Guid ProductId,
    Guid ServiceId,
    string Description,
    DateOnly StartsOn);

public sealed record UpdateWarrantyRequest(
    Guid ProductId,
    Guid ServiceId,
    string Description,
    DateOnly StartsOn);

public sealed record WarrantyDto(
    Guid Id,
    Guid ProductId,
    Guid ServiceId,
    string Description,
    DateOnly StartsOn,
    DateOnly ExpiresOn);
