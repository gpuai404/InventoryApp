using WarrantyIssueXE.Domain.Primitives;
using WarrantyIssueXE.Domain.Snapshots;

namespace WarrantyIssueXE.Domain.ValueObjects;

/// <summary>
/// The one current issuer configuration used for future warranty issuance.
/// It has no identity because the application supports exactly one issuer.
/// </summary>
public sealed record IssuerProfile
{
    public IssuerProfile(
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

    public IssuerSnapshot ToSnapshot() => new(Name, VatNumber, Address, Email, Phone);
}
