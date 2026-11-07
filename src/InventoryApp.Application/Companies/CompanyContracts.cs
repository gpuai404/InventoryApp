namespace InventoryApp.Application.Companies;

public sealed record CompanyContactInput(
    string? Address,
    string? Email,
    string? Phone);

public sealed record CreateCompanyRequest(
    string Name,
    CompanyContactInput Contact);

public sealed record UpdateCompanyRequest(
    string Name,
    CompanyContactInput Contact);

public sealed record CompanyContactDto(
    string? Address,
    string? Email,
    string? Phone);

public sealed record CompanyDto(
    Guid Id,
    string Name,
    CompanyContactDto Contact);
