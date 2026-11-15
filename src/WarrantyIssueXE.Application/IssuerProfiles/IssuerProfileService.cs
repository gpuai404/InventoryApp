using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Application.IssuerProfiles;

public sealed class IssuerProfileService
{
    private readonly IIssuerProfileStore _store;
    private readonly IUnitOfWork _unitOfWork;

    public IssuerProfileService(IIssuerProfileStore store, IUnitOfWork unitOfWork)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<IssuerProfileDto?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        (await _store.GetCurrentAsync(cancellationToken))?.ToDto();

    public async Task<IssuerProfileDto> SaveAsync(
        SaveIssuerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = new IssuerProfile(
            request.Name,
            request.VatNumber,
            request.Address,
            request.Email,
            request.Phone);

        await _store.SaveAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return profile.ToDto();
    }
}
