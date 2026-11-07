using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Application.Abstractions.Persistence;

/// <summary>
/// Write port for product variants.
/// </summary>
public interface IProductVariantWriter : IEntityWriter<ProductVariant, ProductVariantId>
{
}
