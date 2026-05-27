using FSH.Modules.Billing.Contracts.v1.Invoices;
using FSH.Modules.Billing.Contracts.v1.Plans;
using FSH.Modules.Billing.Contracts.v1.Subscriptions;
using FSH.Modules.Billing.Contracts.v1.Usage;
using FSH.Modules.Billing.Features.v1.Invoices.GenerateInvoices;
using FSH.Modules.Billing.Features.v1.Plans.CreatePlan;
using FSH.Modules.Billing.Features.v1.Plans.UpdatePlan;
using FSH.Modules.Billing.Features.v1.Subscriptions.AssignSubscription;
using FSH.Modules.Billing.Features.v1.Usage.CaptureUsageSnapshots;

namespace Billing.Tests.Validators;

public sealed class BillingValidatorsTests
{
    #region CreatePlan

    [Fact]
    public void CreatePlan_Should_Pass_When_Valid()
    {
        var validator = new CreatePlanCommandValidator();

        var result = validator.Validate(new CreatePlanCommand("pro", "Pro", "USD", 49m));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "Pro", "USD", 1)]   // empty key
    [InlineData("pro", "", "USD", 1)]   // empty name
    [InlineData("pro", "Pro", "US", 1)] // currency not length 3
    [InlineData("pro", "Pro", "USD", -1)] // negative price
    public void CreatePlan_Should_Fail_When_Invalid(string key, string name, string currency, decimal price)
    {
        var validator = new CreatePlanCommandValidator();

        var result = validator.Validate(new CreatePlanCommand(key, name, currency, price));

        result.IsValid.ShouldBeFalse();
    }

    #endregion

    #region UpdatePlan

    [Fact]
    public void UpdatePlan_Should_Pass_When_Valid()
    {
        var validator = new UpdatePlanCommandValidator();

        var result = validator.Validate(new UpdatePlanCommand(Guid.CreateVersion7(), "Pro", 10m));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void UpdatePlan_Should_Fail_When_PlanId_Empty()
    {
        var validator = new UpdatePlanCommandValidator();

        var result = validator.Validate(new UpdatePlanCommand(Guid.Empty, "Pro", 10m));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void UpdatePlan_Should_Fail_When_Price_Negative()
    {
        var validator = new UpdatePlanCommandValidator();

        var result = validator.Validate(new UpdatePlanCommand(Guid.CreateVersion7(), "Pro", -1m));

        result.IsValid.ShouldBeFalse();
    }

    #endregion

    #region AssignSubscription

    [Fact]
    public void AssignSubscription_Should_Pass_When_Valid()
    {
        var validator = new AssignSubscriptionCommandValidator();

        var result = validator.Validate(new AssignSubscriptionCommand("tenant-1", "pro"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "pro")]
    [InlineData("tenant-1", "")]
    public void AssignSubscription_Should_Fail_When_Required_Field_Empty(string tenantId, string planKey)
    {
        var validator = new AssignSubscriptionCommandValidator();

        var result = validator.Validate(new AssignSubscriptionCommand(tenantId, planKey));

        result.IsValid.ShouldBeFalse();
    }

    #endregion

    #region GenerateInvoices

    [Theory]
    [InlineData(2026, 1, true)]
    [InlineData(1999, 1, false)]
    [InlineData(2026, 0, false)]
    [InlineData(2026, 13, false)]
    public void GenerateInvoices_Should_Validate_Period_Bounds(int year, int month, bool expectedValid)
    {
        var validator = new GenerateInvoicesCommandValidator();

        var result = validator.Validate(new GenerateInvoicesCommand(year, month));

        result.IsValid.ShouldBe(expectedValid);
    }

    #endregion

    #region CaptureUsageSnapshots

    [Theory]
    [InlineData("tenant-1", 2026, 1, true)]
    [InlineData("", 2026, 1, false)]
    [InlineData("tenant-1", 1999, 1, false)]
    [InlineData("tenant-1", 2026, 13, false)]
    public void CaptureUsageSnapshots_Should_Validate_Inputs(string tenantId, int year, int month, bool expectedValid)
    {
        var validator = new CaptureUsageSnapshotsCommandValidator();

        var result = validator.Validate(new CaptureUsageSnapshotsCommand(tenantId, year, month));

        result.IsValid.ShouldBe(expectedValid);
    }

    #endregion
}
