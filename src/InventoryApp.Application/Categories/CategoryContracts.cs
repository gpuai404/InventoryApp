namespace InventoryApp.Application.Categories;

public sealed record CreateCategoryRequest(
    string Name,
    string? Description = null);

public sealed record UpdateCategoryRequest(
    string Name,
    string? Description = null);

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string? Description);
