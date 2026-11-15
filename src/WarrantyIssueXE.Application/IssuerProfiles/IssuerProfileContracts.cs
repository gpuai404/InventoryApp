namespace WarrantyIssueXE.Application.IssuerProfiles;

public sealed record SaveIssuerProfileRequest(
    string Name,
    string? VatNumber,
    string Address,
    string? Email = null,
    string? Phone = null);

public sealed record IssuerProfileDto(
    string Name,
    string? VatNumber,
    string Address,
    string? Email,
    string? Phone);
