using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Exceptions;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.Primitives;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Domain.Tests;

public sealed class CoreEntityTests
{
    [Fact]
    public void Company_normalizes_contact_details_and_preserves_identity_when_updated()
    {
        var company = new Company(
            "  Contoso  ",
            new CompanyContact(
                "  Avenue des Arts 1  ",
                "  hello@contoso.example  ",
                "  +32 2 555 0100  "));

        var updated = company.Revise(
            "  Fabrikam  ",
            new CompanyContact(
                "  Rue de la Loi 2  ",
                "  hello@fabrikam.example  ",
                "  +32 2 555 0200  "));

        Assert.Equal("Contoso", company.Name);
        Assert.Equal("Avenue des Arts 1", company.Contact.Address);
        Assert.Equal("hello@contoso.example", company.Contact.Email);
        Assert.Equal("+32 2 555 0100", company.Contact.Phone);
        Assert.Equal("Fabrikam", updated.Name);
        Assert.Equal("Rue de la Loi 2", updated.Contact.Address);
        Assert.Equal("hello@fabrikam.example", updated.Contact.Email);
        Assert.Equal("+32 2 555 0200", updated.Contact.Phone);
        Assert.Equal(company.Id, updated.Id);
    }

    [Fact]
    public void Company_rejects_blank_name()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new Company(" ", CompanyContact.Empty));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
        Assert.Equal("name", exception.PropertyName);
    }

    [Fact]
    public void Company_requires_a_contact_value_object()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new Company("Contoso", null!));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
        Assert.Equal("contact", exception.PropertyName);
    }

    [Fact]
    public void Category_normalizes_optional_description()
    {
        var category = new Category("  Electronics  ", "  Devices  ");

        var withoutDescription = category.Revise(category.Name, " ");

        Assert.Equal("Electronics", category.Name);
        Assert.Equal("Devices", category.Description);
        Assert.Null(withoutDescription.Description);
        Assert.Equal(category.Id, withoutDescription.Id);
    }

    [Fact]
    public void Category_rejects_blank_name()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new Category(" "));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
        Assert.Equal("name", exception.PropertyName);
    }

    [Fact]
    public void Product_updates_nondestructively()
    {
        var categoryId = CategoryId.New();
        var replacementCategoryId = CategoryId.New();
        var product = new Product(
            categoryId,
            "  Laptop  ",
            new Money(100m, "EUR"),
            "  Portable computer  ");

        var updated = product.Revise(
            replacementCategoryId,
            "  Notebook  ",
            new Money(125m, "EUR"),
            "  Updated description  ");

        Assert.Equal("Laptop", product.Name);
        Assert.Equal("Portable computer", product.Description);
        Assert.Equal(categoryId, product.CategoryId);
        Assert.Equal(product.Id, updated.Id);
        Assert.Equal(replacementCategoryId, updated.CategoryId);
        Assert.Equal("Notebook", updated.Name);
        Assert.Equal(125m, updated.Price.Amount);
        Assert.Equal("Updated description", updated.Description);
    }

    [Fact]
    public void Product_rejects_blank_name()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new Product(CategoryId.New(), " ", new Money(1m, "EUR")));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
        Assert.Equal("name", exception.PropertyName);
    }

    [Fact]
    public void Product_rejects_text_that_cannot_be_persisted()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new Product(
                CategoryId.New(),
                new string('P', DomainTextLimits.Name + 1),
                new Money(1m, "EUR")));

        Assert.Equal(DomainErrorCodes.TextTooLong, exception.Code);
        Assert.Equal("name", exception.PropertyName);
    }

    [Fact]
    public void Product_rejects_an_overlong_optional_description()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new Product(
                CategoryId.New(),
                "Cable",
                new Money(1m, "EUR"),
                new string('D', DomainTextLimits.Description + 1)));

        Assert.Equal(DomainErrorCodes.TextTooLong, exception.Code);
        Assert.Equal("description", exception.PropertyName);
    }

    [Fact]
    public void Service_references_company_by_id()
    {
        var companyId = CompanyId.New();
        var service = new Service(
            companyId,
            "  Installation  ",
            new WarrantyTerm(24),
            description: "  On-site setup  ");

        var updated = service.Revise(
            "  Premium installation  ",
            new WarrantyTerm(6),
            "  Full on-site setup  ");

        Assert.Equal(companyId, service.CompanyId);
        Assert.Equal("Installation", service.Name);
        Assert.Equal(24, service.WarrantyTerm.Months);
        Assert.Equal("On-site setup", service.Description);
        Assert.Equal(service.Id, updated.Id);
        Assert.Equal("Premium installation", updated.Name);
        Assert.Equal(6, updated.WarrantyTerm.Months);
        Assert.Equal("Full on-site setup", updated.Description);
    }

    [Fact]
    public void Service_rejects_blank_name()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new Service(CompanyId.New(), " ", new WarrantyTerm(12)));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
        Assert.Equal("name", exception.PropertyName);
    }
}
