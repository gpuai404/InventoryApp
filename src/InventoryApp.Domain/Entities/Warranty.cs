using InventoryApp.Domain.Exceptions;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.Primitives;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Domain.Entities;

public sealed class Warranty : Entity<WarrantyId>
{
    private Warranty(
        ProductId productId,
        ServiceId serviceId,
        string description,
        DateOnly startsOn,
        DateOnly expiresOn,
        WarrantyId? id = null)
        : base(id ?? WarrantyId.New())
    {
        ProductId = DomainGuard.RequiredId(productId, nameof(productId));
        ServiceId = DomainGuard.RequiredId(serviceId, nameof(serviceId));

        if (expiresOn < startsOn)
        {
            throw WarrantyExceptions.ExpirationPrecedesStart(nameof(expiresOn));
        }

        Description = DomainGuard.RequiredText(
            description,
            nameof(description),
            DomainTextLimits.Description);
        StartsOn = startsOn;
        ExpiresOn = expiresOn;
    }

    public ProductId ProductId { get; private init; }

    public ServiceId ServiceId { get; private init; }

    public string Description { get; private init; }

    public DateOnly StartsOn { get; private init; }

    public DateOnly ExpiresOn { get; private init; }

    public static Warranty Issue(
        ProductId productId,
        ServiceId serviceId,
        string description,
        DateOnly startsOn,
        WarrantyTerm warrantyTerm,
        WarrantyId? id = null)
    {
        warrantyTerm = DomainGuard.Required(warrantyTerm, nameof(warrantyTerm));

        return new(
            productId,
            serviceId,
            description,
            startsOn,
            warrantyTerm.ExpiresOn(startsOn),
            id);
    }

    /// <summary>
    /// Rehydrates an already-issued warranty. New warranties must be created through
    /// <see cref="Issue"/> so their expiry remains a consequence of the service term.
    /// </summary>
    public static Warranty Reconstitute(
        ProductId productId,
        ServiceId serviceId,
        string description,
        DateOnly startsOn,
        DateOnly expiresOn,
        WarrantyId id) =>
        new(
            productId,
            serviceId,
            description,
            startsOn,
            expiresOn,
            id);

    public bool IsActiveOn(DateOnly date) =>
        date >= StartsOn && date <= ExpiresOn;

    /// <summary>
    /// Reissues this warranty from the selected service policy. The expiry is always
    /// recalculated from the supplied start date and warranty term.
    /// </summary>
    public Warranty Revise(
        ProductId productId,
        ServiceId serviceId,
        string description,
        DateOnly startsOn,
        WarrantyTerm warrantyTerm) =>
        Issue(
            productId,
            serviceId,
            description,
            startsOn,
            warrantyTerm,
            Id);
}
