using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Application.Common;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Application.Companies;

public sealed class CompanyCommandService
{
    private readonly ICompanyReader _reader;
    private readonly ICompanyWriter _writer;
    private readonly IUnitOfWork _unitOfWork;

    public CompanyCommandService(
        ICompanyReader reader,
        ICompanyWriter writer,
        IUnitOfWork unitOfWork)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CompanyDto> CreateAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Contact);

        var company = new Company(
            request.Name,
            request.Contact.ToDomain());
        await _writer.AddAsync(company, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return company.ToDto();
    }

    public async Task<CompanyDto> UpdateAsync(
        Guid companyId,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Contact);

        var typedId = new CompanyId(companyId);
        var company = await _reader.GetRequiredAsync(typedId, cancellationToken);

        var updatedCompany = company.Revise(
            request.Name,
            request.Contact.ToDomain());
        await _writer.UpdateAsync(updatedCompany, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return updatedCompany.ToDto();
    }

    public async Task ArchiveAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var typedId = new CompanyId(companyId);
        await _reader.GetRequiredAsync(typedId, cancellationToken);

        await _writer.ArchiveAsync(typedId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
