using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Primitives;

namespace WarrantyIssueXE.Domain.ValueObjects;

/// <summary>
/// A normalized 15-digit International Mobile Equipment Identity.
/// </summary>
public sealed record Imei
{
    public Imei(string value)
    {
        var normalizedValue = DomainGuard.RequiredText(value, nameof(value));
        if (normalizedValue.Length != DomainTextLimits.Imei ||
            normalizedValue.Any(character => character is < '0' or > '9') ||
            !HasValidCheckDigit(normalizedValue))
        {
            throw ImeiExceptions.Invalid(nameof(value));
        }

        Value = normalizedValue;
    }

    public string Value { get; }

    public override string ToString() => Value;

    private static bool HasValidCheckDigit(string value)
    {
        var sum = 0;
        for (var index = 0; index < value.Length; index++)
        {
            var digit = value[index] - '0';
            if (index % 2 == 1)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
        }

        return sum % 10 == 0;
    }
}
