namespace WarrantyIssueXE.Infrastructure.Persistence;

internal sealed class WarrantyPlanRecord : ArchivableRecord
{
    public Guid Id { get; set; }

    public string Type { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int TermMonths { get; set; }

    public string CoverageDescription { get; set; } = null!;
}
