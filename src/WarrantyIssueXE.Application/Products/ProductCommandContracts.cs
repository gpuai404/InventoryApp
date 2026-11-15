namespace WarrantyIssueXE.Application.Products;

/// <summary>
/// Product catalog command inputs. Product is the only catalog entity.
/// </summary>
public sealed record CreateProductRequest(
    Guid CategoryId,
    string Name,
    decimal PriceAmount,
    string CurrencyCode,
    string? Description = null);

public sealed record UpdateProductRequest(
    Guid CategoryId,
    string Name,
    decimal PriceAmount,
    string CurrencyCode,
    string? Description = null);
