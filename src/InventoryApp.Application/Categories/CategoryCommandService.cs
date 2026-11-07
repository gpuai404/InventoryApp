using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Application.Common;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Application.Categories;

public sealed class CategoryCommandService
{
    private readonly ICategoryReader _reader;
    private readonly ICategoryWriter _writer;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryCommandService(
        ICategoryReader reader,
        ICategoryWriter writer,
        IUnitOfWork unitOfWork)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CategoryDto> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = new Category(request.Name, request.Description);
        await _writer.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return category.ToDto();
    }

    public async Task<CategoryDto> UpdateAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var typedId = new CategoryId(categoryId);
        var category = await _reader.GetRequiredAsync(typedId, cancellationToken);

        var updatedCategory = category.Revise(request.Name, request.Description);

        await _writer.UpdateAsync(updatedCategory, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return updatedCategory.ToDto();
    }

    public async Task ArchiveAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var typedId = new CategoryId(categoryId);
        await _reader.GetRequiredAsync(typedId, cancellationToken);

        await _writer.ArchiveAsync(typedId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
