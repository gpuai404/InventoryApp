using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Application.Abstractions.Persistence;

public interface IWarrantyReader : IEntityReader<Warranty, WarrantyId>
{
}
