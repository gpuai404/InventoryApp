namespace WarrantyIssueXE.Infrastructure.Persistence;

internal sealed class ProductVariantRecord : ArchivableRecord
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string? Sku { get; set; }

    public List<ProductBarcodeRecord> Barcodes { get; set; } = [];
}
