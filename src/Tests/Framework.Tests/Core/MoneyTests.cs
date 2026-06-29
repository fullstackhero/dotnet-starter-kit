using FSH.Framework.Core.Domain;

namespace Framework.Tests.Core;

public sealed class MoneyTests
{
    #region Construction

    [Fact]
    public void Ctor_Should_NormalizeCurrencyToUpper_When_LowercaseProvided()
    {
        var money = new Money(10.50m, "usd");

        money.Currency.ShouldBe("USD");
        money.Amount.ShouldBe(10.50m);
    }

    [Fact]
    public void Ctor_Should_AllowZeroAmount_When_AmountIsZero()
    {
        var money = new Money(0m, "USD");

        money.Amount.ShouldBe(0m);
    }

    [Fact]
    public void Ctor_Should_AllowNegativeAmount_When_AmountIsNegative()
    {
        // Money is a ledger primitive: debits and credit reversals are negative amounts.
        var money = new Money(-12.34m, "USD");

        money.Amount.ShouldBe(-12.34m);
    }

    [Fact]
    public void Zero_Should_ReturnZeroAmountWithDefaultCurrency_When_NoCurrencySupplied()
    {
        Money zero = Money.Zero();

        zero.Amount.ShouldBe(0m);
        zero.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Zero_Should_UseSuppliedCurrency_When_CurrencyProvided()
    {
        Money zero = Money.Zero("eur");

        zero.Currency.ShouldBe("EUR");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Should_Throw_When_CurrencyIsBlank(string currency)
    {
        Should.Throw<ArgumentException>(() => new Money(1m, currency));
    }

    [Fact]
    public void Ctor_Should_Throw_When_CurrencyIsNull()
    {
        Should.Throw<ArgumentException>(() => new Money(1m, null!));
    }

    #endregion

    #region Equality

    [Fact]
    public void Equality_Should_BeEqual_When_AmountAndNormalizedCurrencyMatch()
    {
        var a = new Money(5m, "usd");
        var b = new Money(5m, "USD");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Equality_Should_NotBeEqual_When_AmountsDiffer()
    {
        (new Money(5m, "USD") != new Money(6m, "USD")).ShouldBeTrue();
    }

    [Fact]
    public void Equality_Should_NotBeEqual_When_CurrenciesDiffer()
    {
        new Money(5m, "USD").ShouldNotBe(new Money(5m, "EUR"));
    }

    [Fact]
    public void With_Should_ProduceModifiedCopy_When_AmountChangedViaWithExpression()
    {
        var original = new Money(5m, "USD");

        Money modified = original with { Amount = 9m };

        modified.Amount.ShouldBe(9m);
        modified.Currency.ShouldBe("USD");
        original.Amount.ShouldBe(5m);
    }

    #endregion
}
