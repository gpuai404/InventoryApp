using InventoryApp.Application.Abstractions.Persistence;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Application.Services;

public sealed class ServiceQueryService
{
    private readonly IServiceReader _reader;

    public ServiceQueryService(IServiceReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<ServiceDto?> GetAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var service = await _reader.GetByIdAsync(
            new ServiceId(serviceId),
            cancellationToken);

        return service?.ToDto();
    }

    public async Task<IReadOnlyList<ServiceDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var services = await _reader.ListAsync(cancellationToken);
        return services.Select(service => service.ToDto()).ToArray();
    }
}
