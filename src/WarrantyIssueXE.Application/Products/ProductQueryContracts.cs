namespace WarrantyIssueXE.Application.Products;

/// <summary>
/// Product catalog read model exposed to Presentation.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    decimal PriceAmount,
    string CurrencyCode,
    string? Description);
