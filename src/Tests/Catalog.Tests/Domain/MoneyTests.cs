using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class MoneyTests
{
    #region Construction - Happy Path

    [Fact]
    public void Ctor_Should_NormalizeCurrencyToUpper_When_LowercaseProvided()
    {
        // Arrange / Act
        var money = new Money(10.50m, "usd");

        // Assert
        money.Currency.ShouldBe("USD");
        money.Amount.ShouldBe(10.50m);
    }

    [Fact]
    public void Ctor_Should_AllowZeroAmount_When_AmountIsZero()
    {
        // Arrange / Act
        var money = new Money(0m, "USD");

        // Assert
        money.Amount.ShouldBe(0m);
    }

    [Fact]
    public void Zero_Should_ReturnZeroAmountWithDefaultCurrency_When_NoCurrencySupplied()
    {
        // Arrange / Act
        Money zero = Money.Zero();

        // Assert
        zero.Amount.ShouldBe(0m);
        zero.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Zero_Should_UseSuppliedCurrency_When_CurrencyProvided()
    {
        // Arrange / Act
        Money zero = Money.Zero("eur");

        // Assert
        zero.Currency.ShouldBe("EUR");
    }

    #endregion

    #region Construction - Guards

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Should_Throw_When_CurrencyIsBlank(string currency)
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() => new Money(1m, currency));
    }

    [Fact]
    public void Ctor_Should_Throw_When_CurrencyIsNull()
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() => new Money(1m, null!));
    }

    [Fact]
    public void Ctor_Should_Throw_When_AmountIsNegative()
    {
        // Act / Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new Money(-0.01m, "USD"));
    }

    #endregion

    #region Equality / Operators

    [Fact]
    public void Equality_Should_BeEqual_When_AmountAndNormalizedCurrencyMatch()
    {
        // Arrange
        var a = new Money(5m, "usd");
        var b = new Money(5m, "USD");

        // Assert - currency normalization makes these value-equal
        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Equality_Should_NotBeEqual_When_AmountsDiffer()
    {
        // Arrange
        var a = new Money(5m, "USD");
        var b = new Money(6m, "USD");

        // Assert
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_Should_NotBeEqual_When_CurrenciesDiffer()
    {
        // Arrange
        var a = new Money(5m, "USD");
        var b = new Money(5m, "EUR");

        // Assert
        a.ShouldNotBe(b);
    }

    [Fact]
    public void With_Should_ProduceModifiedCopy_When_AmountChangedViaWithExpression()
    {
        // Arrange
        var original = new Money(5m, "USD");

        // Act
        Money modified = original with { Amount = 9m };

        // Assert
        modified.Amount.ShouldBe(9m);
        modified.Currency.ShouldBe("USD");
        original.Amount.ShouldBe(5m);
    }

    #endregion
}
