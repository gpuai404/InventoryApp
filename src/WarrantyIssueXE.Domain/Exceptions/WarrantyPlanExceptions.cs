namespace WarrantyIssueXE.Domain.Exceptions;

internal static class WarrantyPlanExceptions
{
    public static DomainRuleViolationException TypeDoesNotMatch(
        string propertyName,
        string planType,
        string requestedType) =>
        new(
            DomainErrorCodes.WarrantyPlanTypeMismatch,
            $"A {planType} warranty plan cannot provide coverage for a {requestedType} warranty.",
            propertyName);
}
