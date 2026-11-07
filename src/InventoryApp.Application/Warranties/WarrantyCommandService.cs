using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Application.Common;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Application.Warranties;

public sealed class WarrantyCommandService
{
    private readonly IProductReader _productReader;
    private readonly IServiceReader _serviceReader;
    private readonly IWarrantyReader _reader;
    private readonly IWarrantyWriter _writer;
    private readonly IUnitOfWork _unitOfWork;

    public WarrantyCommandService(
        IProductReader productReader,
        IServiceReader serviceReader,
        IWarrantyReader reader,
        IWarrantyWriter writer,
        IUnitOfWork unitOfWork)
    {
        _productReader = productReader ?? throw new ArgumentNullException(nameof(productReader));
        _serviceReader = serviceReader ?? throw new ArgumentNullException(nameof(serviceReader));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<WarrantyDto> CreateAsync(
        CreateWarrantyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var productId = new ProductId(request.ProductId);
        await _productReader.GetRequiredAsync(productId, cancellationToken);
        var serviceId = new ServiceId(request.ServiceId);
        var service = await _serviceReader.GetRequiredAsync(serviceId, cancellationToken);

        var warranty = Warranty.Issue(
            productId,
            serviceId,
            request.Description,
            request.StartsOn,
            service.WarrantyTerm);

        await _writer.AddAsync(warranty, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return warranty.ToDto();
    }

    public async Task<WarrantyDto> UpdateAsync(
        Guid warrantyId,
        UpdateWarrantyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var typedId = new WarrantyId(warrantyId);
        var warranty = await _reader.GetRequiredAsync(typedId, cancellationToken);

        var productId = new ProductId(request.ProductId);
        await _productReader.GetRequiredAsync(productId, cancellationToken);
        var serviceId = new ServiceId(request.ServiceId);
        var service = await _serviceReader.GetRequiredAsync(serviceId, cancellationToken);

        var updatedWarranty = warranty.Revise(
            productId,
            serviceId,
            request.Description,
            request.StartsOn,
            service.WarrantyTerm);

        await _writer.UpdateAsync(updatedWarranty, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return updatedWarranty.ToDto();
    }

    public async Task ArchiveAsync(
        Guid warrantyId,
        CancellationToken cancellationToken = default)
    {
        var typedId = new WarrantyId(warrantyId);
        await _reader.GetRequiredAsync(typedId, cancellationToken);

        await _writer.ArchiveAsync(typedId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
