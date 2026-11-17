namespace WarrantyIssueXE.Application.Categories;

/// <summary>
/// Signals that a category cannot be archived while active catalog products
/// still depend on it.
/// </summary>
public sealed class CategoryInUseException : Exception
{
    public CategoryInUseException(Guid categoryId)
        : base(
            $"Category with identifier '{categoryId:D}' cannot be archived while active products " +
            "reference it. Reassign or archive those products first.")
    {
        CategoryId = categoryId;
    }

    public Guid CategoryId { get; }
}
