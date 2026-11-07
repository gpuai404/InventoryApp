using InventoryApp.Domain.Exceptions;
using InventoryApp.Domain.Identifiers;
using InventoryApp.Domain.Primitives;
using InventoryApp.Domain.ValueObjects;

namespace InventoryApp.Domain.Tests;

public sealed class ValueObjectTests
{
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
    public void Barcode_normalizes_text()
    {
        var barcode = new ProductBarcode(" 123 ", "  Box ");

        Assert.Equal("123", barcode.Code);
        Assert.Equal("Box", barcode.PackagingDescription);
    }

    [Fact]
    public void Barcode_without_packaging_description_has_no_description()
    {
        var barcode = new ProductBarcode("123");

        Assert.Null(barcode.PackagingDescription);
    }

    [Fact]
    public void Empty_barcode_is_rejected()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new ProductBarcode(" "));

        Assert.Equal(DomainErrorCodes.RequiredValue, exception.Code);
        Assert.Equal("code", exception.PropertyName);
    }

    [Fact]
    public void Barcode_rejects_text_that_cannot_be_persisted()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => new ProductBarcode(
                new string('1', DomainTextLimits.ProductBarcodeCode + 1)));

        Assert.Equal(DomainErrorCodes.TextTooLong, exception.Code);
        Assert.Equal("code", exception.PropertyName);
    }

    [Fact]
    public void Equal_barcodes_compare_equal()
    {
        var first = new ProductBarcode("123", "Box");
        var second = new ProductBarcode("123", "Box");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Warranty_term_calculates_the_final_inclusive_day()
    {
        var expiration = new WarrantyTerm(24).ExpiresOn(new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(2027, 12, 31), expiration);
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
