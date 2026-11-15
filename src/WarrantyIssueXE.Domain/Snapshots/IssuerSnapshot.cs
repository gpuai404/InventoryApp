using WarrantyIssueXE.Domain.Primitives;

namespace WarrantyIssueXE.Domain.Snapshots;

/// <summary>
/// Issuer facts copied into a certificate at issuance. These values are never
/// read back from the current issuer profile.
/// </summary>
public sealed record IssuerSnapshot
{
    public IssuerSnapshot(
        string name,
        string? vatNumber,
        string address,
        string? email = null,
        string? phone = null)
    {
        Name = DomainGuard.RequiredText(name, nameof(name), DomainTextLimits.Name);
        VatNumber = DomainGuard.OptionalText(vatNumber, nameof(vatNumber), DomainTextLimits.VatNumber);
        Address = DomainGuard.RequiredText(address, nameof(address), DomainTextLimits.ContactAddress);
        Email = DomainGuard.OptionalText(email, nameof(email), DomainTextLimits.ContactEmail);
        Phone = DomainGuard.OptionalText(phone, nameof(phone), DomainTextLimits.ContactPhone);
    }

    public string Name { get; }
    public string? VatNumber { get; }
    public string Address { get; }
    public string? Email { get; }
    public string? Phone { get; }
}
