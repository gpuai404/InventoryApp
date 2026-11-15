namespace WarrantyIssueXE.Application.Products;

/// <summary>
/// Indicates that a barcode is already reserved by another product variant.
/// The same application-level conflict is used for both the preflight check and
/// a database uniqueness race.
/// </summary>
public sealed class ProductBarcodeConflictException(string barcodeCode)
    : InvalidOperationException("The barcode is already assigned to another product variant.")
{
    public string BarcodeCode { get; } = barcodeCode;
}
