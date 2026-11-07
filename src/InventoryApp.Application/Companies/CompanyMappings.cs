using InventoryApp.Domain.Entities;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Application.Companies;

internal static class CompanyMappings
{
    public static CompanyDto ToDto(this Company company) =>
        new(
            company.Id.Value,
            company.Name,
            new CompanyContactDto(
                company.Contact.Address,
                company.Contact.Email,
                company.Contact.Phone));

    public static CompanyContact ToDomain(this CompanyContactInput contact) =>
        new(contact.Address, contact.Email, contact.Phone);
}
