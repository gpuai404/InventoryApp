namespace WarrantyIssueXE.Application.Warranties;

/// <summary>
/// Signals that another operation voided the certificate after it was read.
/// Presentation can map this to a stable conflict response.
/// </summary>
public sealed class WarrantyLifecycleConflictException(Guid warrantyId)
    : Exception($"Warranty '{warrantyId:D}' is no longer in the issued state.")
{
    public Guid WarrantyId { get; } = warrantyId;
}
