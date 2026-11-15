using WarrantyIssueXE.Application.Abstractions.Persistence;
using WarrantyIssueXE.Application.Abstractions.Certificates;
using WarrantyIssueXE.Application.Abstractions.Time;
using WarrantyIssueXE.Application.Common;
using WarrantyIssueXE.Domain.Entities;
using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.ValueObjects;
using WarrantyIssueXE.Domain.WarrantyDetails;

namespace WarrantyIssueXE.Application.Warranties;

/// <summary>
/// Coordinates warranty issuance and the one-way void lifecycle. It deliberately
/// has no editable or archive operation for an issued certificate.
/// </summary>
public sealed class WarrantyCommandService
{
    private readonly IProductVariantReader _variantReader;
    private readonly IProductReader _productReader;
    private readonly IIssuerProfileStore _issuerProfileStore;
    private readonly IWarrantyPlanReader _planReader;
    private readonly IWarrantyReader _warrantyReader;
    private readonly IWarrantyWriter _warrantyWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICertificateNumberGenerator _certificateNumberGenerator;
    private readonly IBusinessClock _clock;

    public WarrantyCommandService(
        IProductVariantReader variantReader,
        IProductReader productReader,
        IIssuerProfileStore issuerProfileStore,
        IWarrantyPlanReader planReader,
        IWarrantyReader warrantyReader,
        IWarrantyWriter warrantyWriter,
        IUnitOfWork unitOfWork,
        ICertificateNumberGenerator certificateNumberGenerator,
        IBusinessClock clock)
    {
        _variantReader = variantReader ?? throw new ArgumentNullException(nameof(variantReader));
        _productReader = productReader ?? throw new ArgumentNullException(nameof(productReader));
        _issuerProfileStore = issuerProfileStore ?? throw new ArgumentNullException(nameof(issuerProfileStore));
        _planReader = planReader ?? throw new ArgumentNullException(nameof(planReader));
        _warrantyReader = warrantyReader ?? throw new ArgumentNullException(nameof(warrantyReader));
        _warrantyWriter = warrantyWriter ?? throw new ArgumentNullException(nameof(warrantyWriter));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _certificateNumberGenerator = certificateNumberGenerator ?? throw new ArgumentNullException(nameof(certificateNumberGenerator));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<WarrantyDto> IssueForSaleAsync(
        IssueWarrantyForSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Coverage);

        var context = await LoadIssuanceContextAsync(request.ProductVariantId, cancellationToken);
        var coverage = await ResolveCoverageAsync(
            request.Coverage,
            WarrantyType.ProductSale,
            cancellationToken);
        var currentTime = _clock.GetCurrent();

        var warranty = Warranty.IssueForSale(
            _certificateNumberGenerator.Generate(),
            context.Variant.Id,
            coverage.WarrantyPlanId,
            request.SerialNumber,
            currentTime.UtcNow,
            currentTime.BusinessDate,
            ToCustomerSnapshot(request.Customer),
            new ProductSnapshot(context.Product.Name, context.Variant.Name, context.Variant.Sku),
            context.Issuer.ToSnapshot(),
            coverage.Snapshot,
            new SaleDetails(
                request.PurchasedOn,
                new Money(request.PurchasePriceAmount, request.PurchasePriceCurrencyCode)));

        await _warrantyWriter.AddAsync(warranty, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return warranty.ToDto();
    }

    public async Task<WarrantyDto> IssueForRepairAsync(
        IssueWarrantyForRepairRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Coverage);

        var context = await LoadIssuanceContextAsync(request.ProductVariantId, cancellationToken);
        var coverage = await ResolveCoverageAsync(
            request.Coverage,
            WarrantyType.Repair,
            cancellationToken);
        var currentTime = _clock.GetCurrent();
        var replacedParts = request.ReplacedParts?
            .Select(part => part is null
                ? throw new ApplicationValidationException(
                    "Replaced-parts collection cannot contain null items.",
                    nameof(request.ReplacedParts))
                : new ReplacedPartSnapshot(part.Description, part.PartNumber))
            .ToArray();

        var warranty = Warranty.IssueForRepair(
            _certificateNumberGenerator.Generate(),
            context.Variant.Id,
            coverage.WarrantyPlanId,
            request.SerialNumber,
            currentTime.UtcNow,
            currentTime.BusinessDate,
            ToCustomerSnapshot(request.Customer),
            new ProductSnapshot(context.Product.Name, context.Variant.Name, context.Variant.Sku),
            context.Issuer.ToSnapshot(),
            coverage.Snapshot,
            new RepairDetails(request.RepairedOn, request.WorkDescription, replacedParts));

        await _warrantyWriter.AddAsync(warranty, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return warranty.ToDto();
    }

    public async Task<WarrantyDto> VoidAsync(
        Guid warrantyId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var typedWarrantyId = new WarrantyId(warrantyId);
        var warranty = await _warrantyReader
            .GetByIdAsync(typedWarrantyId, cancellationToken)
            .RequireFoundAsync(typedWarrantyId.Value);
        var voided = warranty.Void(reason, _clock.GetCurrent().UtcNow);

        await _warrantyWriter.VoidAsync(voided, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return voided.ToDto();
    }

    private async Task<IssuanceContext> LoadIssuanceContextAsync(
        Guid productVariantId,
        CancellationToken cancellationToken)
    {
        var typedVariantId = new ProductVariantId(productVariantId);
        var variant = await _variantReader
            .GetByIdAsync(typedVariantId, cancellationToken)
            .RequireFoundAsync(typedVariantId.Value);
        var product = await _productReader
            .GetByIdAsync(variant.ProductId, cancellationToken)
            .RequireFoundAsync(variant.ProductId.Value);
        var issuer = await _issuerProfileStore.GetCurrentAsync(cancellationToken)
            ?? throw new ApplicationValidationException(
                "Configure the issuer profile before issuing a warranty.",
                "issuerProfile");

        return new IssuanceContext(variant, product, issuer);
    }

    private async Task<ResolvedCoverage> ResolveCoverageAsync(
        CoverageInput coverage,
        WarrantyType type,
        CancellationToken cancellationToken)
    {
        if (coverage is WarrantyPlanCoverageInput planCoverage)
        {
            var typedPlanId = new WarrantyPlanId(planCoverage.WarrantyPlanId);
            var plan = await _planReader
                .GetByIdAsync(typedPlanId, cancellationToken)
                .RequireFoundAsync(typedPlanId.Value);

            return new ResolvedCoverage(
                typedPlanId,
                plan.CreateCoverageFor(type));
        }

        if (coverage is ExplicitCoverageInput explicitCoverage)
        {
            var term = new WarrantyTerm(explicitCoverage.TermMonths);
            return new ResolvedCoverage(
                null,
                type == WarrantyType.ProductSale
                    ? CoverageSnapshot.ForProductSale(term, explicitCoverage.CoverageDescription)
                    : CoverageSnapshot.ForRepair(term, explicitCoverage.CoverageDescription));
        }

        throw new ApplicationValidationException(
            "The coverage source is not supported.",
            nameof(coverage));
    }

    private static CustomerSnapshot ToCustomerSnapshot(CustomerInput input) =>
        new(input.FullName, input.Email, input.Phone, input.Address);

    private sealed record IssuanceContext(ProductVariant Variant, Product Product, IssuerProfile Issuer);

    private sealed record ResolvedCoverage(WarrantyPlanId? WarrantyPlanId, CoverageSnapshot Snapshot);
}
