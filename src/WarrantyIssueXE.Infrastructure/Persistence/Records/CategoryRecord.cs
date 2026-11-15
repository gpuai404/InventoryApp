namespace WarrantyIssueXE.Infrastructure.Persistence;

internal sealed class CategoryRecord : ArchivableRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}
