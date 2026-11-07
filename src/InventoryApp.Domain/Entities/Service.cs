using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.Primitives;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Domain.Entities;

public sealed class Service : Entity<ServiceId>
{
    public Service(
        CompanyId companyId,
        string name,
        WarrantyTerm warrantyTerm,
        string? description = null,
        ServiceId? id = null)
        : base(id ?? ServiceId.New())
    {
        CompanyId = DomainGuard.RequiredId(companyId, nameof(companyId));
        Name = DomainGuard.RequiredText(name, nameof(name), DomainTextLimits.Name);
        WarrantyTerm = DomainGuard.Required(warrantyTerm, nameof(warrantyTerm));
        Description = DomainGuard.OptionalText(
            description,
            nameof(description),
            DomainTextLimits.Description);
    }

    public CompanyId CompanyId { get; private init; }

    public string Name { get; private init; }

    /// <summary>
    /// The warranty duration granted when this service is used to issue a warranty.
    /// </summary>
    public WarrantyTerm WarrantyTerm { get; private init; }

    public string? Description { get; private init; }

    /// <summary>
    /// Replaces the service's editable state while preserving its identity and owner.
    /// </summary>
    public Service Revise(
        string name,
        WarrantyTerm warrantyTerm,
        string? description) =>
        new(CompanyId, name, warrantyTerm, description, Id);
}
