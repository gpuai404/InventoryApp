using WarrantyIssueXE.Domain.Exceptions;
using WarrantyIssueXE.Domain.Primitives;
using WarrantyIssueXE.Domain.ValueObjects;

namespace WarrantyIssueXE.Domain.Tests;

public sealed class ValueObjectTests
{
    [Fact]
    public void Issuer_profile_is_immutable_and_validated()
    {
        var profile = new IssuerProfile("  Contoso  ", "  BE 0123  ", "Street 1");

        Assert.Equal("Contoso", profile.Name);
        Assert.Equal("BE 0123", profile.ToSnapshot().VatNumber);
        Assert.Throws<DomainValidationException>(() => new IssuerProfile(" ", null, "Street 1"));
    }

    [Fact]
    public void Money_normalizes_currency_code()
    {
        var money = new Money(12.50m, " eur ");

        Assert.Equal(12.50m, money.Amount);
        Assert.Equal("EUR", money.CurrencyCode);
    }

    [Fact]
    public void Zero_money_is_valid()
    {
        var money = new Money(0m, "USD");

        Assert.Equal(0m, money.Amount);
        Assert.Equal("USD", money.CurrencyCode);
    }

    [Fact]
    public void Negative_money_is_rejected()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new Money(-1m, "EUR"));

        Assert.Equal(DomainErrorCodes.NegativeMoneyAmount, exception.Code);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("EURO")]
    [InlineData("US1")]
    public void Invalid_currency_code_is_rejected(string currencyCode)
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new Money(1m, currencyCode));

        Assert.Equal(DomainErrorCodes.InvalidCurrencyCode, exception.Code);
        Assert.Equal(nameof(currencyCode), exception.PropertyName);
    }

    [Fact]
    public void Equal_money_values_compare_equal()
    {
        var first = new Money(10m, "eur");
        var second = new Money(10m, "EUR");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Imei_normalizes_and_preserves_a_valid_identifier()
    {
        var imei = new Imei(" 490154203237518 ");

        Assert.Equal("490154203237518", imei.Value);
        Assert.Equal("490154203237518", imei.ToString());
    }

    [Theory]
    [InlineData("49015420323751")]
    [InlineData("4901542032375188")]
    [InlineData("49015420323751A")]
    [InlineData("490154203237519")]
    public void Invalid_imei_is_rejected(string value)
    {
        var exception = Assert.Throws<DomainValidationException>(() => new Imei(value));

        Assert.Equal(DomainErrorCodes.InvalidImei, exception.Code);
        Assert.Equal(nameof(value), exception.PropertyName);
    }

    [Fact]
    public void Warranty_term_calculates_the_final_inclusive_day()
    {
        var expiration = new WarrantyTerm(24).ExpiresOn(new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(2027, 12, 31), expiration);
    }

    [Theory]
    [InlineData(2026, 1, 1, 1, 2026, 1, 31)]
    [InlineData(2026, 1, 31, 1, 2026, 2, 27)]
    [InlineData(2024, 2, 29, 1, 2024, 3, 28)]
    [InlineData(2026, 7, 1, 12, 2027, 6, 30)]
    public void Warranty_term_preserves_documented_month_end_expiry_behavior(
        int startYear,
        int startMonth,
        int startDay,
        int months,
        int expiryYear,
        int expiryMonth,
        int expiryDay)
    {
        var expiration = new WarrantyTerm(months).ExpiresOn(
            new DateOnly(startYear, startMonth, startDay));

        Assert.Equal(new DateOnly(expiryYear, expiryMonth, expiryDay), expiration);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Warranty_term_requires_at_least_one_month(int months)
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new WarrantyTerm(months));

        Assert.Equal(DomainErrorCodes.WarrantyTermMonthsInvalid, exception.Code);
        Assert.Equal("months", exception.PropertyName);
    }
}
