namespace WarrantyIssueXE.Infrastructure.Persistence;

/// <summary>
/// Persistence audit state shared by master records that can be retired from
/// normal application use without being physically deleted.
/// </summary>
internal abstract class ArchivableRecord
{
    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }
}
