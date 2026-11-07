namespace InventoryApp.Domain.Primitives;

/// <summary>
/// Text limits that are part of the domain contract and are mirrored by persistence.
/// </summary>
public static class DomainTextLimits
{
    public const int Name = 200;
    public const int Description = 2000;
    public const int CompanyAddress = 500;
    public const int CompanyEmail = 320;
    public const int CompanyPhone = 50;
    public const int CurrencyCode = 3;
    public const int ProductVariantSku = 100;
    public const int ProductBarcodeCode = 200;
    public const int ProductBarcodePackagingDescription = 500;
}
