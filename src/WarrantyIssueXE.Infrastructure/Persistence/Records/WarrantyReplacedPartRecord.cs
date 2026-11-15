namespace WarrantyIssueXE.Infrastructure.Persistence;

internal sealed class WarrantyReplacedPartRecord
{
    public Guid WarrantyId { get; set; }

    public int Position { get; set; }

    public string Description { get; set; } = null!;

    public string? PartNumber { get; set; }
}
