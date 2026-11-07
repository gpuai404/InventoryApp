namespace InventoryApp.Infrastructure.Persistence;

// These records are persistence models, not domain entities. They are shaped
// for relational storage and deliberately live outside Domain.

internal abstract class SoftDeletableRecord
{
    // Persistence lifecycle state; immutable domain entities deliberately do not expose it.
    public DateTime? DeletedOnUtc { get; set; }
}

internal sealed class CategoryRecord : SoftDeletableRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}

internal sealed class CompanyRecord : SoftDeletableRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }
}

internal sealed class ProductRecord : SoftDeletableRecord
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public decimal PriceAmount { get; set; }

    public string PriceCurrencyCode { get; set; } = null!;

    public string? Description { get; set; }
}

internal sealed class ProductVariantRecord : SoftDeletableRecord
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string? Sku { get; set; }

    public List<ProductBarcodeRecord> Barcodes { get; set; } = [];
}

internal sealed class ProductBarcodeRecord
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Code { get; set; } = null!;

    public string? PackagingDescription { get; set; }
}

internal sealed class ServiceRecord : SoftDeletableRecord
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public int WarrantyTermMonths { get; set; }

    public string? Description { get; set; }
}

internal sealed class WarrantyRecord : SoftDeletableRecord
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid ServiceId { get; set; }

    public string Description { get; set; } = null!;

    public DateOnly StartsOn { get; set; }

    public DateOnly ExpiresOn { get; set; }

}
