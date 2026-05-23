using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Domain;

namespace Billing.Tests.Domain;

public sealed class SubscriptionTests
{
    #region Happy Path

    [Fact]
    public void Create_Should_Start_Active_With_Utc_Start_When_Valid()
    {
        var local = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Unspecified);

        var sub = Subscription.Create("tenant-1", Guid.CreateVersion7(), local);

        sub.TenantId.ShouldBe("tenant-1");
        sub.Status.ShouldBe(SubscriptionStatus.Active);
        sub.EndUtc.ShouldBeNull();
        sub.StartUtc.Kind.ShouldBe(DateTimeKind.Utc);
        sub.CreatedAtUtc.ShouldNotBe(default);
    }

    [Fact]
    public void Suspend_Should_Set_Status_Suspended_And_Touch_UpdatedAt()
    {
        var sub = Subscription.Create("tenant-1", Guid.CreateVersion7(), DateTime.UtcNow);

        sub.Suspend();

        sub.Status.ShouldBe(SubscriptionStatus.Suspended);
        sub.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Reactivate_Should_Set_Status_Active_After_Suspend()
    {
        var sub = Subscription.Create("tenant-1", Guid.CreateVersion7(), DateTime.UtcNow);
        sub.Suspend();

        sub.Reactivate();

        sub.Status.ShouldBe(SubscriptionStatus.Active);
        sub.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Cancel_Should_Set_Status_Cancelled_And_Normalize_EndUtc_To_Utc()
    {
        var sub = Subscription.Create("tenant-1", Guid.CreateVersion7(), DateTime.UtcNow);
        var localEnd = new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Unspecified);

        sub.Cancel(localEnd);

        sub.Status.ShouldBe(SubscriptionStatus.Cancelled);
        sub.EndUtc.ShouldNotBeNull();
        sub.EndUtc!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        sub.UpdatedAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region Exceptions

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_TenantId_Blank(string tenantId)
    {
        Should.Throw<ArgumentException>(() => Subscription.Create(tenantId, Guid.CreateVersion7(), DateTime.UtcNow));
    }

    [Fact]
    public void Create_Should_Throw_When_TenantId_Null()
    {
        Should.Throw<ArgumentException>(() => Subscription.Create(null!, Guid.CreateVersion7(), DateTime.UtcNow));
    }

    [Fact]
    public void Create_Should_Throw_When_PlanId_Empty()
    {
        Should.Throw<ArgumentException>(() => Subscription.Create("tenant-1", Guid.Empty, DateTime.UtcNow));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Reactivate_From_Cancelled_Should_Clear_Active_But_Leave_EndUtc()
    {
        var sub = Subscription.Create("tenant-1", Guid.CreateVersion7(), DateTime.UtcNow);
        sub.Cancel(DateTime.UtcNow);

        sub.Reactivate();

        // Reactivate only flips status; the recorded EndUtc is intentionally left in place.
        sub.Status.ShouldBe(SubscriptionStatus.Active);
        sub.EndUtc.ShouldNotBeNull();
    }

    #endregion
}
