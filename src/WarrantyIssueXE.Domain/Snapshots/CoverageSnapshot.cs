using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Primitives;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Domain.Snapshots;

/// <summary>
/// The warranty terms and certificate wording frozen at issuance time.
/// </summary>
public sealed record CoverageSnapshot
{
    /// <summary>
    /// Standard certificate wording for a product-sale warranty. Coverage
    /// descriptions carry the product-specific limitations, so callers do not
    /// need to invent a second, inconsistent scope statement.
    /// </summary>
    public const string ProductScopeStatement =
        "This warranty covers the product identified in this certificate, subject to the coverage description and limitations.";

    /// <summary>
    /// Mandatory wording for every repair warranty. It is owned by the domain
    /// so callers cannot make repair coverage appear to cover the whole product.
    /// </summary>
    public const string RepairScopeStatement =
        "This warranty covers only the completed repair work and parts replaced as part of that repair. " +
        "It does not cover the entire product or restart or extend any original product warranty.";

    private CoverageSnapshot(
        WarrantyTerm term,
        CoverageScope scope,
        string coverageDescription,
        string scopeStatement)
    {
        Term = DomainGuard.Required(term, nameof(term));
        Scope = DomainGuard.DefinedEnum(scope, nameof(scope));
        CoverageDescription = DomainGuard.RequiredText(
            coverageDescription,
            nameof(coverageDescription),
            DomainTextLimits.Description);
        ScopeStatement = DomainGuard.RequiredText(
            scopeStatement,
            nameof(scopeStatement),
            DomainTextLimits.WarrantyScopeStatement);

        if (Scope == CoverageScope.RepairWorkAndReplacedParts &&
            !string.Equals(ScopeStatement, RepairScopeStatement, StringComparison.Ordinal))
        {
            throw WarrantyExceptions.RepairScopeStatementInvalid(nameof(scopeStatement));
        }
    }

    public WarrantyTerm Term { get; }

    public CoverageScope Scope { get; }

    public string CoverageDescription { get; }

    public string ScopeStatement { get; }

    public static CoverageSnapshot ForProductSale(
        WarrantyTerm term,
        string coverageDescription) =>
        new(term, CoverageScope.Product, coverageDescription, ProductScopeStatement);

    public static CoverageSnapshot ForRepair(WarrantyTerm term, string coverageDescription) =>
        new(
            term,
            CoverageScope.RepairWorkAndReplacedParts,
            coverageDescription,
            RepairScopeStatement);

    /// <summary>
    /// Persistence-only factory for validating and reconstituting historical
    /// coverage. The coverage description is historical, while the repair
    /// limitation remains a mandatory aggregate invariant.
    /// </summary>
    public static CoverageSnapshot Reconstitute(
        WarrantyTerm term,
        CoverageScope scope,
        string coverageDescription,
        string scopeStatement) =>
        new(term, scope, coverageDescription, scopeStatement);
}
