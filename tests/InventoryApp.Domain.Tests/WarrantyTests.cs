using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Exceptions;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Domain.Tests;

public sealed class WarrantyTests
{
    [Fact]
    public void Product_warranty_derives_expiration_from_service_term()
    {
        var productId = ProductId.New();
        var serviceId = ServiceId.New();

        var warranty = Warranty.Issue(
            productId,
            serviceId,
            "Parts and labor",
            new DateOnly(2026, 1, 1),
            new WarrantyTerm(24));

        Assert.Equal(productId, warranty.ProductId);
        Assert.Equal(serviceId, warranty.ServiceId);
        Assert.Equal("Parts and labor", warranty.Description);
        Assert.Equal(new DateOnly(2027, 12, 31), warranty.ExpiresOn);
    }

    [Fact]
    public void Missing_service_is_rejected()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Warranty.Issue(
                ProductId.New(),
                default,
                "Description",
                new DateOnly(2026, 1, 1),
                new WarrantyTerm(12)));

        Assert.Equal(DomainErrorCodes.EmptyIdentifier, exception.Code);
        Assert.Equal("serviceId", exception.PropertyName);
    }

    [Fact]
    public void Reconstitution_rejects_a_non_chronological_period()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(
            () => Warranty.Reconstitute(
                ProductId.New(),
                ServiceId.New(),
                "Description",
                new DateOnly(2026, 12, 31),
                new DateOnly(2026, 1, 1),
                WarrantyId.New()));

        Assert.Equal(DomainErrorCodes.WarrantyPeriodInvalid, exception.Code);
        Assert.Equal("expiresOn", exception.PropertyName);
    }

    [Fact]
    public void Active_period_is_inclusive_at_both_boundaries()
    {
        var startsOn = new DateOnly(2026, 1, 1);
        var warranty = Warranty.Issue(
            ProductId.New(),
            ServiceId.New(),
            "Description",
            startsOn,
            new WarrantyTerm(12));

        Assert.True(warranty.IsActiveOn(startsOn));
        Assert.True(warranty.IsActiveOn(new DateOnly(2026, 12, 31)));
        Assert.False(warranty.IsActiveOn(startsOn.AddDays(-1)));
        Assert.False(warranty.IsActiveOn(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void Revising_warranty_recalculates_expiration_from_the_selected_service_term()
    {
        var originalServiceId = ServiceId.New();
        var warranty = Warranty.Issue(
            ProductId.New(),
            originalServiceId,
            "New sale warranty",
            new DateOnly(2026, 1, 1),
            new WarrantyTerm(24));

        var updated = warranty.Revise(
            warranty.ProductId,
            ServiceId.New(),
            "Repair warranty",
            new DateOnly(2027, 1, 1),
            new WarrantyTerm(6));

        Assert.Equal(originalServiceId, warranty.ServiceId);
        Assert.Equal(new DateOnly(2027, 12, 31), warranty.ExpiresOn);
        Assert.Equal(warranty.Id, updated.Id);
        Assert.NotEqual(warranty.ServiceId, updated.ServiceId);
        Assert.Equal(new DateOnly(2027, 6, 30), updated.ExpiresOn);
        Assert.Equal("Repair warranty", updated.Description);
    }

}
