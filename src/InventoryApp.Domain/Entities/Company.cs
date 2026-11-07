using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.Primitives;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Domain.Entities;

public sealed class Company : Entity<CompanyId>
{
    public Company(
        string name,
        CompanyContact contact,
        CompanyId? id = null)
        : base(id ?? CompanyId.New())
    {
        Name = DomainGuard.RequiredText(name, nameof(name), DomainTextLimits.Name);
        Contact = DomainGuard.Required(contact, nameof(contact));
    }

    public string Name { get; private init; }

    public CompanyContact Contact { get; private init; }

    /// <summary>
    /// Replaces the company's editable state while preserving its identity.
    /// </summary>
    public Company Revise(
        string name,
        CompanyContact contact) =>
        new(name, contact, Id);
}
