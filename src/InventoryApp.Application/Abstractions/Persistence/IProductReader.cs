using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Application.Abstractions.Persistence;

/// <summary>
/// Read port for products. Query consumers do not receive write operations.
/// </summary>
public interface IProductReader : IEntityReader<Product, ProductId>
{
}
