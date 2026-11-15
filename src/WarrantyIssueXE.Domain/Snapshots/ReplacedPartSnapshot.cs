using WarrantyIssueXE.Domain.Primitives;

namespace WarrantyIssueXE.Domain.Snapshots;

/// <summary>
/// A part description recorded as an immutable fact of a completed repair.
/// </summary>
public sealed record ReplacedPartSnapshot
{
    public ReplacedPartSnapshot(string description, string? partNumber = null)
    {
        Description = DomainGuard.RequiredText(
            description,
            nameof(description),
            DomainTextLimits.Description);
        PartNumber = DomainGuard.OptionalText(
            partNumber,
            nameof(partNumber),
            DomainTextLimits.ReplacedPartNumber);
    }

    public string Description { get; }

    public string? PartNumber { get; }
}
