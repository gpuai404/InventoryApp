using WarrantyIssueXE.Domain.Enums;

namespace WarrantyIssueXE.Application.WarrantyPlans;

public sealed record CreateWarrantyPlanRequest(
    WarrantyType Type,
    string Name,
    int TermMonths,
    string CoverageDescription);

public sealed record UpdateWarrantyPlanRequest(
    string Name,
    int TermMonths,
    string CoverageDescription);

public sealed record WarrantyPlanDto(
    Guid Id,
    WarrantyType Type,
    string Name,
    int TermMonths,
    string CoverageDescription);
