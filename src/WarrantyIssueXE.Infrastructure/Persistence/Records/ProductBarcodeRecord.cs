namespace WarrantyIssueXE.Infrastructure.Persistence;

internal sealed class ProductBarcodeRecord
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Code { get; set; } = null!;

    public string? PackagingDescription { get; set; }
}
