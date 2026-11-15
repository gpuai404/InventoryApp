using System.Collections.ObjectModel;
using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Primitives;
using WarrantyIssueXE.Domain.Snapshots;

namespace WarrantyIssueXE.Domain.WarrantyDetails;

/// <summary>
/// Completed repair facts. This is a value object with custom equality because
/// its replaced-parts sequence has ordered, structural semantics.
/// </summary>
public sealed class RepairDetails : IEquatable<RepairDetails>
{
    private readonly ReadOnlyCollection<ReplacedPartSnapshot> _replacedParts;

    public RepairDetails(
        DateOnly repairedOn,
        string workDescription,
        IEnumerable<ReplacedPartSnapshot>? replacedParts = null)
    {
        RepairedOn = DomainGuard.RequiredDate(repairedOn, nameof(repairedOn));
        WorkDescription = DomainGuard.RequiredText(
            workDescription,
            nameof(workDescription),
            DomainTextLimits.Description);

        var copiedParts = replacedParts?.ToArray() ?? [];
        if (copiedParts.Any(part => part is null))
        {
            throw DomainValidationExceptions.NullCollectionItem(nameof(replacedParts));
        }

        _replacedParts = Array.AsReadOnly(copiedParts);
    }

    public DateOnly RepairedOn { get; }

    public string WorkDescription { get; }

    public IReadOnlyList<ReplacedPartSnapshot> ReplacedParts => _replacedParts;

    public bool Equals(RepairDetails? other) =>
        other is not null &&
        RepairedOn == other.RepairedOn &&
        string.Equals(WorkDescription, other.WorkDescription, StringComparison.Ordinal) &&
        _replacedParts.SequenceEqual(other._replacedParts);

    public override bool Equals(object? obj) => Equals(obj as RepairDetails);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(RepairedOn);
        hash.Add(WorkDescription, StringComparer.Ordinal);
        foreach (var replacedPart in _replacedParts)
        {
            hash.Add(replacedPart);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(RepairDetails? left, RepairDetails? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    public static bool operator !=(RepairDetails? left, RepairDetails? right) => !(left == right);
}
