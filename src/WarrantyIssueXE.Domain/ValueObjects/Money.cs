using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Primitives;

namespace WarrantyIssueXE.Domain.ValueObjects;

public sealed record Money
{
    public Money(decimal amount, string currencyCode)
    {
        if (amount < 0)
        {
            throw MoneyExceptions.NegativeAmount(nameof(amount));
        }

        var normalizedCurrencyCode = DomainGuard.RequiredText(currencyCode, nameof(currencyCode)).ToUpperInvariant();
        if (normalizedCurrencyCode.Length != DomainTextLimits.CurrencyCode ||
            normalizedCurrencyCode.Any(c => c is < 'A' or > 'Z'))
        {
            throw MoneyExceptions.InvalidCurrencyCode(nameof(currencyCode));
        }

        Amount = amount;
        CurrencyCode = normalizedCurrencyCode;
    }

    public decimal Amount { get; }

    public string CurrencyCode { get; }
}
