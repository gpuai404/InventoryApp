using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Application.Abstractions.Persistence;

public interface ICategoryWriter : IEntityWriter<Category, CategoryId>
{
}
