using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Application.Companies;

public sealed class CompanyQueryService
{
    private readonly ICompanyReader _reader;

    public CompanyQueryService(ICompanyReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<CompanyDto?> GetAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var company = await _reader.GetByIdAsync(
            new CompanyId(companyId),
            cancellationToken);

        return company?.ToDto();
    }

    public async Task<IReadOnlyList<CompanyDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var companies = await _reader.ListAsync(cancellationToken);
        return companies.Select(company => company.ToDto()).ToArray();
    }
}
