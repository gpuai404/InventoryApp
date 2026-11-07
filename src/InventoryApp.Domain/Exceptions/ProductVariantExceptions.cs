namespace InventoryApp.Domain.Exceptions;

/// <summary>
/// Creates exceptions for product-variant barcode rules.
/// Keeping error construction here gives callers stable error codes while leaving
/// entities focused on enforcing their invariants.
/// </summary>
internal static class ProductVariantExceptions
{
    public static DomainRuleViolationException BarcodeAlreadyAssigned(
        string barcodeCode,
        string propertyName) =>
        new(
            DomainErrorCodes.DuplicateBarcode,
            $"Barcode '{barcodeCode}' is already assigned to this product variant.",
            propertyName);

    public static DomainRuleViolationException BarcodeNotAssigned(
        string barcodeCode,
        string propertyName) =>
        new(
            DomainErrorCodes.BarcodeNotFound,
            $"Barcode '{barcodeCode}' is not assigned to this product variant.",
            propertyName);

    public static DomainValidationException BarcodeCollectionContainsNull(
        string propertyName) =>
        new(
            DomainErrorCodes.NullCollectionItem,
            "Barcode collection cannot contain null values.",
            propertyName);

    public static DomainRuleViolationException BarcodeCollectionContainsDuplicates(
        string propertyName) =>
        new(
            DomainErrorCodes.DuplicateBarcode,
            "A product variant cannot contain duplicate barcodes.",
            propertyName);
}
