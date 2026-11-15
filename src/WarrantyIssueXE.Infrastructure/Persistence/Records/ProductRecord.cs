namespace WarrantyIssueXE.Infrastructure.Persistence;

internal sealed class ProductRecord : ArchivableRecord
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public decimal PriceAmount { get; set; }

    public string PriceCurrencyCode { get; set; } = null!;

    public string? Description { get; set; }
}
