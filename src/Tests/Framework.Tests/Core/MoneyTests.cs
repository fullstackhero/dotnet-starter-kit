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

    #region Arithmetic

    [Fact]
    public void Add_Should_SumAmounts_When_CurrenciesMatch()
    {
        var sum = new Money(10m, "USD").Add(new Money(2.50m, "usd"));

        sum.Amount.ShouldBe(12.50m);
        sum.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Subtract_Should_DiffAmounts_When_CurrenciesMatch()
    {
        var diff = new Money(10m, "USD").Subtract(new Money(2.50m, "USD"));

        diff.Amount.ShouldBe(7.50m);
    }

    [Fact]
    public void Subtract_Should_AllowNegativeResult_When_RightExceedsLeft()
    {
        var diff = new Money(2m, "USD").Subtract(new Money(5m, "USD"));

        diff.Amount.ShouldBe(-3m);
    }

    [Fact]
    public void Multiply_Should_ScaleAmountAndKeepCurrency()
    {
        var product = new Money(3m, "USD").Multiply(4m);

        product.Amount.ShouldBe(12m);
        product.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Round_Should_RoundAwayFromZero()
    {
        new Money(1.005m, "USD").Round(2).Amount.ShouldBe(1.01m);
        new Money(-1.005m, "USD").Round(2).Amount.ShouldBe(-1.01m);
    }

    [Fact]
    public void Add_Should_Throw_When_CurrenciesDiffer()
    {
        Should.Throw<InvalidOperationException>(() => new Money(1m, "USD").Add(new Money(1m, "EUR")));
    }

    [Fact]
    public void Subtract_Should_Throw_When_CurrenciesDiffer()
    {
        Should.Throw<InvalidOperationException>(() => new Money(1m, "USD").Subtract(new Money(1m, "EUR")));
    }

    [Fact]
    public void Operators_Should_MirrorNamedMethods()
    {
        var a = new Money(10m, "USD");
        var b = new Money(4m, "USD");

        (a + b).Amount.ShouldBe(14m);
        (a - b).Amount.ShouldBe(6m);
        (a * 2m).Amount.ShouldBe(20m);
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
