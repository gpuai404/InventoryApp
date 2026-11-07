using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Application.Categories;

public sealed class CategoryQueryService
{
    private readonly ICategoryReader _reader;

    public CategoryQueryService(ICategoryReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<CategoryDto?> GetAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await _reader.GetByIdAsync(
            new CategoryId(categoryId),
            cancellationToken);

        return category?.ToDto();
    }

    public async Task<IReadOnlyList<CategoryDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await _reader.ListAsync(cancellationToken);
        return categories.Select(category => category.ToDto()).ToArray();
    }
}
