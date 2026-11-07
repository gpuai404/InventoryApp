using InventoryApp.Domain.Primitives;

namespace InventoryApp.Domain.ValueObjects;

/// <summary>
/// Optional contact details belonging to a company.
/// The value object is always present on a company, even when no contact detail is known.
/// </summary>
public sealed record CompanyContact
{
    public static CompanyContact Empty { get; } = new(null, null, null);

    public CompanyContact(string? address, string? email, string? phone)
    {
        Address = DomainGuard.OptionalText(
            address,
            nameof(address),
            DomainTextLimits.CompanyAddress);
        Email = DomainGuard.OptionalText(
            email,
            nameof(email),
            DomainTextLimits.CompanyEmail);
        Phone = DomainGuard.OptionalText(
            phone,
            nameof(phone),
            DomainTextLimits.CompanyPhone);
    }

    public string? Address { get; }

    public string? Email { get; }

    public string? Phone { get; }
}
