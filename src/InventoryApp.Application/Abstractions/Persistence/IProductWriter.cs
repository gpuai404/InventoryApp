using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Application.Abstractions.Persistence;

/// <summary>
/// Write port for products. Infrastructure implements this port for SQLite or
/// another persistence mechanism.
/// </summary>
public interface IProductWriter : IEntityWriter<Product, ProductId>
{
}
