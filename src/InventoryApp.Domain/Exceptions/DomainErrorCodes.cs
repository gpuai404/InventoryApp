namespace InventoryApp.Domain.Exceptions;

public static class DomainErrorCodes
{
    public const string RequiredValue = "domain.required_value";
    public const string EmptyIdentifier = "domain.empty_identifier";
    public const string NullCollectionItem = "domain.null_collection_item";
    public const string TextTooLong = "domain.text_too_long";

    public const string DuplicateBarcode = "product_variant.duplicate_barcode";
    public const string BarcodeNotFound = "product_variant.barcode_not_found";

    public const string NegativeMoneyAmount = "money.negative_amount";
    public const string InvalidCurrencyCode = "money.invalid_currency_code";

    public const string WarrantyPeriodInvalid = "warranty.period_invalid";
    public const string WarrantyTermMonthsInvalid = "warranty_term.months_invalid";
    public const string WarrantyTermCannotBeApplied = "warranty_term.cannot_be_applied";
}
