using WarrantyIssueXE.Domain.Primitives;

namespace WarrantyIssueXE.Domain.Snapshots;

/// <summary>
/// Customer facts captured for a certificate. This is intentionally not a live
/// customer profile.
/// </summary>
public sealed record CustomerSnapshot
{
    public CustomerSnapshot(
        string fullName,
        string? email = null,
        string? phone = null,
        string? address = null)
    {
        FullName = DomainGuard.RequiredText(fullName, nameof(fullName), DomainTextLimits.Name);
        Email = DomainGuard.OptionalText(email, nameof(email), DomainTextLimits.ContactEmail);
        Phone = DomainGuard.OptionalText(phone, nameof(phone), DomainTextLimits.ContactPhone);
        Address = DomainGuard.OptionalText(address, nameof(address), DomainTextLimits.ContactAddress);
    }

    public string FullName { get; }

    public string? Email { get; }

    public string? Phone { get; }

    public string? Address { get; }
}
