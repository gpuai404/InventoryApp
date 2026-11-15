using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Application.IssuerProfiles;

internal static class IssuerProfileMappings
{
    public static IssuerProfileDto ToDto(this IssuerProfile profile) =>
        new(profile.Name, profile.VatNumber, profile.Address, profile.Email, profile.Phone);
}
