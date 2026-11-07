using InventoryApp.Domain.Primitives;

namespace InventoryApp.Domain.ValueObjects;

public sealed record ProductBarcode
{
    public ProductBarcode(string code, string? packagingDescription = null)
    {
        Code = DomainGuard.RequiredText(
            code,
            nameof(code),
            DomainTextLimits.ProductBarcodeCode);
        PackagingDescription = DomainGuard.OptionalText(
            packagingDescription,
            nameof(packagingDescription),
            DomainTextLimits.ProductBarcodePackagingDescription);
    }

    public string Code { get; }

    public string? PackagingDescription { get; }
}
