using WarrantyIssueXE.Application.Abstractions.Certificates;
using WarrantyIssueXE.Application.Abstractions.Persistence;
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
/// Coordinates the two supported issuance use cases. Issued warranties expose
/// no update, correction, cancellation, or void operation.
/// </summary>
public sealed class WarrantyCommandService
{
    private readonly IProductReader _productReader;
    private readonly ICategoryReader _categoryReader;
    private readonly IIssuerProfileStore _issuerProfileStore;
    private readonly IWarrantyPlanReader _planReader;
    private readonly IWarrantyWriter _warrantyWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICertificateNumberGenerator _certificateNumberGenerator;
    private readonly IBusinessClock _clock;

    public WarrantyCommandService(
        IProductReader productReader,
        ICategoryReader categoryReader,
        IIssuerProfileStore issuerProfileStore,
        IWarrantyPlanReader planReader,
        IWarrantyWriter warrantyWriter,
        IUnitOfWork unitOfWork,
        ICertificateNumberGenerator certificateNumberGenerator,
        IBusinessClock clock)
    {
        _productReader = productReader ?? throw new ArgumentNullException(nameof(productReader));
        _categoryReader = categoryReader ?? throw new ArgumentNullException(nameof(categoryReader));
        _issuerProfileStore = issuerProfileStore ?? throw new ArgumentNullException(nameof(issuerProfileStore));
        _planReader = planReader ?? throw new ArgumentNullException(nameof(planReader));
        _warrantyWriter = warrantyWriter ?? throw new ArgumentNullException(nameof(warrantyWriter));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _certificateNumberGenerator = certificateNumberGenerator ?? throw new ArgumentNullException(nameof(certificateNumberGenerator));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<WarrantyDto> IssueProductSaleWarrantyAsync(
        IssueProductSaleWarrantyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Coverage);

        var context = await LoadIssuanceContextAsync(request.ProductId, cancellationToken);
        var coverage = await ResolveCoverageAsync(
            request.Coverage,
            WarrantyType.ProductSale,
            cancellationToken);
        var currentTime = _clock.GetCurrent();

        var warranty = Warranty.IssueForSale(
            _certificateNumberGenerator.Generate(),
            coverage.WarrantyPlanId,
            ToImei(request.Imei),
            currentTime.UtcNow,
            currentTime.BusinessDate,
            ToCustomerSnapshot(request.Customer),
            context.Product.ToSnapshot(context.Category.Name),
            context.Issuer.ToSnapshot(),
            coverage.Snapshot,
            new SaleDetails(request.PurchasedOn));

        await _warrantyWriter.AddAsync(warranty, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return warranty.ToDto();
    }

    public async Task<WarrantyDto> IssueRepairWarrantyAsync(
        IssueRepairWarrantyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Coverage);

        var context = await LoadIssuanceContextAsync(request.ProductId, cancellationToken);
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
            coverage.WarrantyPlanId,
            ToImei(request.Imei),
            currentTime.UtcNow,
            currentTime.BusinessDate,
            ToCustomerSnapshot(request.Customer),
            context.Product.ToSnapshot(context.Category.Name),
            context.Issuer.ToSnapshot(),
            coverage.Snapshot,
            new RepairDetails(request.RepairedOn, request.WorkDescription, replacedParts));

        await _warrantyWriter.AddAsync(warranty, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return warranty.ToDto();
    }

    private async Task<IssuanceContext> LoadIssuanceContextAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var typedProductId = new ProductId(productId);
        var product = await _productReader
            .GetByIdAsync(typedProductId, cancellationToken)
            .RequireFoundAsync(typedProductId.Value);
        var category = await _categoryReader
            .GetByIdAsync(product.CategoryId, cancellationToken)
            .RequireFoundAsync(product.CategoryId.Value);
        var issuer = await _issuerProfileStore.GetCurrentAsync(cancellationToken)
            ?? throw new ApplicationValidationException(
                "Configure the issuer profile before issuing a warranty.",
                "issuerProfile");

        return new IssuanceContext(product, category, issuer);
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

            return new ResolvedCoverage(typedPlanId, plan.CreateCoverageFor(type));
        }

        if (coverage is ExplicitCoverageInput explicitCoverage)
        {
            var term = new WarrantyTerm(explicitCoverage.TermMonths);
            var snapshot = type == WarrantyType.ProductSale
                ? CoverageSnapshot.ForProductSale(term, explicitCoverage.CoverageDescription)
                : CoverageSnapshot.ForRepair(term, explicitCoverage.CoverageDescription);

            return new ResolvedCoverage(null, snapshot);
        }

        throw new ApplicationValidationException(
            "The coverage source is not supported.",
            nameof(coverage));
    }

    private static Imei? ToImei(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new Imei(value);

    private static CustomerSnapshot ToCustomerSnapshot(CustomerInput input) =>
        new(input.FullName, input.Email, input.Phone, input.Address);

    private sealed record IssuanceContext(Product Product, Category Category, IssuerProfile Issuer);

    private sealed record ResolvedCoverage(WarrantyPlanId? WarrantyPlanId, CoverageSnapshot Snapshot);
}
