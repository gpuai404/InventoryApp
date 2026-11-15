using WarrantyIssueXE.Domain.Entities;

namespace WarrantyIssueXE.Application.Categories;

internal static class CategoryMappings
{
    public static CategoryDto ToDto(this Category category) =>
        new(category.Id.Value, category.Name, category.Description);
}
