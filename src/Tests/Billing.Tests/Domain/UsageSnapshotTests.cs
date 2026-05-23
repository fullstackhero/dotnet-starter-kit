using FSH.Framework.Shared.Quota;
using FSH.Modules.Billing.Domain;

namespace Billing.Tests.Domain;

public sealed class UsageSnapshotTests
{
    #region Happy Path

    [Fact]
    public void Capture_Should_Record_Used_And_Limit()
    {
        var snap = UsageSnapshot.Capture("tenant-1", 2026, 1, QuotaResource.ApiCalls, 1500, 1000);

        snap.TenantId.ShouldBe("tenant-1");
        snap.Resource.ShouldBe(QuotaResource.ApiCalls);
        snap.UsedUnits.ShouldBe(1500);
        snap.LimitUnits.ShouldBe(1000);
        snap.CapturedAtUtc.ShouldNotBe(default);
    }

    #endregion

    #region Exceptions

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Capture_Should_Throw_When_TenantId_Blank(string tenantId)
    {
        Should.Throw<ArgumentException>(() =>
            UsageSnapshot.Capture(tenantId, 2026, 1, QuotaResource.Users, 1, 1));
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Capture_Should_Throw_When_Year_Out_Of_Range(int year)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            UsageSnapshot.Capture("tenant-1", year, 1, QuotaResource.Users, 1, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Capture_Should_Throw_When_Month_Out_Of_Range(int month)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            UsageSnapshot.Capture("tenant-1", 2026, month, QuotaResource.Users, 1, 1));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Overage_Should_Be_Positive_Difference_When_Over_Limit()
    {
        var snap = UsageSnapshot.Capture("tenant-1", 2026, 1, QuotaResource.ApiCalls, 1500, 1000);

        snap.Overage.ShouldBe(500);
    }

    [Fact]
    public void Overage_Should_Be_Zero_When_Under_Limit()
    {
        var snap = UsageSnapshot.Capture("tenant-1", 2026, 1, QuotaResource.ApiCalls, 800, 1000);

        snap.Overage.ShouldBe(0);
    }

    [Fact]
    public void Overage_Should_Be_Zero_When_Exactly_At_Limit()
    {
        var snap = UsageSnapshot.Capture("tenant-1", 2026, 1, QuotaResource.ApiCalls, 1000, 1000);

        snap.Overage.ShouldBe(0);
    }

    #endregion
}
