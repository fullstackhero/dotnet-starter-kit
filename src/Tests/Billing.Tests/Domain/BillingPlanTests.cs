using FSH.Framework.Shared.Quota;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Domain;

namespace Billing.Tests.Domain;

public sealed class BillingPlanTests
{
    #region Billing interval / term

    [Fact]
    public void Create_Should_Default_To_Monthly_Interval()
    {
        var plan = BillingPlan.Create("pro", "Pro", "USD", 30m);

        plan.Interval.ShouldBe(PlanInterval.Monthly);
        plan.TermMonths.ShouldBe(1);
        plan.TermPrice.Amount.ShouldBe(30m);
    }

    [Fact]
    public void Yearly_Plan_Should_Use_AnnualPrice_When_Set()
    {
        var plan = BillingPlan.Create("pro-yr", "Pro Annual", "USD", 30m, interval: PlanInterval.Yearly, annualPrice: 300m);

        plan.Interval.ShouldBe(PlanInterval.Yearly);
        plan.TermMonths.ShouldBe(12);
        plan.TermPrice.Amount.ShouldBe(300m);
    }

    [Fact]
    public void Yearly_Plan_Should_Fall_Back_To_Twelve_Times_Monthly()
    {
        var plan = BillingPlan.Create("pro-yr", "Pro Annual", "USD", 30m, interval: PlanInterval.Yearly);

        plan.TermPrice.Amount.ShouldBe(360m);
    }

    [Fact]
    public void Create_Should_Throw_When_AnnualPrice_Negative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            BillingPlan.Create("pro", "Pro", "USD", 10m, interval: PlanInterval.Yearly, annualPrice: -1m));
    }

    [Fact]
    public void Update_Should_Replace_Interval_And_AnnualPrice()
    {
        var plan = BillingPlan.Create("pro", "Pro", "USD", 30m);

        plan.Update("Pro", 30m, null, PlanInterval.Yearly, 300m);

        plan.Interval.ShouldBe(PlanInterval.Yearly);
        plan.AnnualPrice!.Amount.ShouldBe(300m);
        plan.TermPrice.Amount.ShouldBe(300m);
    }

    #endregion

    #region Happy Path

    [Fact]
    public void Create_Should_Lowercase_Key_And_Uppercase_Currency()
    {
        var plan = BillingPlan.Create("Pro", "Pro Plan", "usd", 49m);

        plan.Key.ShouldBe("pro");
        plan.Currency.ShouldBe("USD");
        plan.Name.ShouldBe("Pro Plan");
        plan.IsActive.ShouldBeTrue();
        plan.MonthlyBasePrice.Amount.ShouldBe(49m);
    }

    [Fact]
    public void Create_Should_Seed_OverageRates_When_Provided()
    {
        var rates = new Dictionary<QuotaResource, decimal>
        {
            [QuotaResource.ApiCalls] = 0.01m,
            [QuotaResource.StorageBytes] = 0.05m
        };

        var plan = BillingPlan.Create("pro", "Pro", "USD", 10m, rates);

        plan.OverageRates.Count.ShouldBe(2);
        plan.GetOverageRate(QuotaResource.ApiCalls).ShouldBe(0.01m);
        plan.GetOverageRate(QuotaResource.StorageBytes).ShouldBe(0.05m);
    }

    [Fact]
    public void GetOverageRate_Should_Return_Zero_When_Resource_Not_Configured()
    {
        var plan = BillingPlan.Create("free", "Free", "USD", 0m);

        plan.GetOverageRate(QuotaResource.Users).ShouldBe(0m);
    }

    [Fact]
    public void Update_Should_Replace_Name_Price_And_OverageRates()
    {
        var plan = BillingPlan.Create("pro", "Pro", "USD", 10m, new Dictionary<QuotaResource, decimal>
        {
            [QuotaResource.ApiCalls] = 0.01m
        });

        var newRates = new Dictionary<QuotaResource, decimal>
        {
            [QuotaResource.Users] = 2m
        };
        plan.Update("Pro Max", 99m, newRates);

        plan.Name.ShouldBe("Pro Max");
        plan.MonthlyBasePrice.Amount.ShouldBe(99m);
        plan.GetOverageRate(QuotaResource.ApiCalls).ShouldBe(0m);
        plan.GetOverageRate(QuotaResource.Users).ShouldBe(2m);
        plan.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Update_Should_Clear_OverageRates_When_Null_Passed()
    {
        var plan = BillingPlan.Create("pro", "Pro", "USD", 10m, new Dictionary<QuotaResource, decimal>
        {
            [QuotaResource.ApiCalls] = 0.01m
        });

        plan.Update("Pro", 10m, null);

        plan.OverageRates.ShouldBeEmpty();
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_False_And_Touch_UpdatedAt()
    {
        var plan = BillingPlan.Create("pro", "Pro", "USD", 10m);

        plan.Deactivate();

        plan.IsActive.ShouldBeFalse();
        plan.UpdatedAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region Exceptions

    [Theory]
    [InlineData("", "name", "USD")]
    [InlineData("key", "", "USD")]
    [InlineData("key", "name", "")]
    public void Create_Should_Throw_When_Required_String_Blank(string key, string name, string currency)
    {
        Should.Throw<ArgumentException>(() => BillingPlan.Create(key, name, currency, 1m));
    }

    [Fact]
    public void Create_Should_Throw_When_Price_Negative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BillingPlan.Create("pro", "Pro", "USD", -1m));
    }

    [Fact]
    public void Update_Should_Throw_When_Name_Blank()
    {
        var plan = BillingPlan.Create("pro", "Pro", "USD", 10m);

        Should.Throw<ArgumentException>(() => plan.Update("  ", 10m, null));
    }

    [Fact]
    public void Update_Should_Throw_When_Price_Negative()
    {
        var plan = BillingPlan.Create("pro", "Pro", "USD", 10m);

        Should.Throw<ArgumentOutOfRangeException>(() => plan.Update("Pro", -5m, null));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_Should_Allow_Zero_Price()
    {
        var plan = BillingPlan.Create("free", "Free", "USD", 0m);

        plan.MonthlyBasePrice.Amount.ShouldBe(0m);
    }

    #endregion
}
