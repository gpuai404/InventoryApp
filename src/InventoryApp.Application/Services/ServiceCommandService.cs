using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Application.Common;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Application.Services;

public sealed class ServiceCommandService
{
    private readonly ICompanyReader _companyReader;
    private readonly IServiceReader _reader;
    private readonly IServiceWriter _writer;
    private readonly IUnitOfWork _unitOfWork;

    public ServiceCommandService(
        ICompanyReader companyReader,
        IServiceReader reader,
        IServiceWriter writer,
        IUnitOfWork unitOfWork)
    {
        _companyReader = companyReader ?? throw new ArgumentNullException(nameof(companyReader));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ServiceDto> CreateAsync(
        CreateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var companyId = new CompanyId(request.CompanyId);
        await _companyReader.GetRequiredAsync(companyId, cancellationToken);

        var service = new Service(
            companyId,
            request.Name,
            new WarrantyTerm(request.WarrantyTermMonths),
            request.Description);

        await _writer.AddAsync(service, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return service.ToDto();
    }

    public async Task<ServiceDto> UpdateAsync(
        Guid serviceId,
        UpdateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var typedId = new ServiceId(serviceId);
        var service = await _reader.GetRequiredAsync(typedId, cancellationToken);

        var updatedService = service.Revise(
            request.Name,
            new WarrantyTerm(request.WarrantyTermMonths),
            request.Description);

        await _writer.UpdateAsync(updatedService, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return updatedService.ToDto();
    }

    public async Task ArchiveAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var typedId = new ServiceId(serviceId);
        await _reader.GetRequiredAsync(typedId, cancellationToken);

        await _writer.ArchiveAsync(typedId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

}
