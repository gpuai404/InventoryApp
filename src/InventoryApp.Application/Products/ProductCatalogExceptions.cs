using InventoryApp.Domain.Exceptions;

namespace InventoryApp.Application.Products;

internal static class ProductCatalogExceptions
{
    public static DomainRuleViolationException BarcodeAlreadyAssigned(
        string barcodeCode,
        string propertyName) =>
        new(
            DomainErrorCodes.DuplicateBarcode,
            $"Barcode '{barcodeCode}' is already assigned to another product variant.",
            propertyName);
}
