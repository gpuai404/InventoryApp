using InventoryApp.Domain.Exceptions;
using InventoryApp.Domain.Identifiers;

namespace InventoryApp.Domain.Primitives;

/// <summary>
/// Common structural validation used by domain constructors and value objects.
/// Aggregate-specific business rules belong beside their exception factories in
/// the Exceptions namespace rather than in this generic guard.
/// </summary>
internal static class DomainGuard
{
    public static string RequiredText(
        string? value,
        string parameterName,
        int? maximumLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw DomainValidationExceptions.RequiredValue(parameterName);
        }

        var normalizedValue = value.Trim();
        EnsureMaximumLength(normalizedValue, parameterName, maximumLength);
        return normalizedValue;
    }

    public static string? OptionalText(
        string? value,
        string parameterName,
        int? maximumLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();
        EnsureMaximumLength(normalizedValue, parameterName, maximumLength);
        return normalizedValue;
    }

    public static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw DomainValidationExceptions.EmptyIdentifier(parameterName);
        }

        return value;
    }

    public static TId RequiredId<TId>(TId value, string parameterName)
        where TId : struct, IEntityId
    {
        if (value.Value == Guid.Empty)
        {
            throw DomainValidationExceptions.EmptyIdentifier(parameterName);
        }

        return value;
    }

    public static T Required<T>(T? value, string parameterName)
        where T : class
    {
        if (value is null)
        {
            throw DomainValidationExceptions.RequiredValue(parameterName);
        }

        return value;
    }

    private static void EnsureMaximumLength(
        string value,
        string parameterName,
        int? maximumLength)
    {
        if (maximumLength is null || value.Length <= maximumLength)
        {
            return;
        }

        throw DomainValidationExceptions.TextExceedsMaximumLength(parameterName, maximumLength.Value);
    }
}
