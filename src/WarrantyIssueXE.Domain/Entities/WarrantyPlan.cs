using WarrantyIssueXE.Domain.Enums;
using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Identifiers;
using WarrantyIssueXE.Domain.Primitives;
using WarrantyIssueXE.Domain.Snapshots;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Domain.Entities;

/// <summary>
/// A reusable application-wide warranty policy. It is policy configuration, not
/// an issued warranty, so revising it never changes historical certificates.
/// </summary>
public sealed class WarrantyPlan : Entity<WarrantyPlanId>
{
    public WarrantyPlan(
        WarrantyType type,
        string name,
        WarrantyTerm term,
        string coverageDescription,
        WarrantyPlanId? id = null)
        : base(id ?? WarrantyPlanId.New())
    {
        Type = DomainGuard.DefinedEnum(type, nameof(type));
        Name = DomainGuard.RequiredText(name, nameof(name), DomainTextLimits.Name);
        Term = DomainGuard.Required(term, nameof(term));
        CoverageDescription = DomainGuard.RequiredText(
            coverageDescription,
            nameof(coverageDescription),
            DomainTextLimits.Description);
    }

    public WarrantyType Type { get; private init; }

    public string Name { get; private init; }

    public WarrantyTerm Term { get; private init; }

    public string CoverageDescription { get; private init; }

    public CoverageSnapshot CreateCoverageFor(WarrantyType warrantyType)
    {
        var validatedType = DomainGuard.DefinedEnum(warrantyType, nameof(warrantyType));
        if (validatedType != Type)
        {
            throw WarrantyPlanExceptions.TypeDoesNotMatch(
                nameof(warrantyType),
                Type.ToString(),
                validatedType.ToString());
        }

        return Type == WarrantyType.ProductSale
            ? CoverageSnapshot.ForProductSale(Term, CoverageDescription)
            : CoverageSnapshot.ForRepair(Term, CoverageDescription);
    }

    /// <summary>
    /// Replaces policy details while preserving this plan's identity and its
    /// workflow. A sale plan cannot be revised into a repair plan or vice versa.
    /// </summary>
    public WarrantyPlan Revise(
        string name,
        WarrantyTerm term,
        string coverageDescription) =>
        new(Type, name, term, coverageDescription, Id);
}
