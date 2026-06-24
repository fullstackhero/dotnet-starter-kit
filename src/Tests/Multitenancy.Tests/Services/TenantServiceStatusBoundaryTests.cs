using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Multitenancy;
using FSH.Modules.Multitenancy.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Multitenancy.Tests.Services;

/// <summary>
/// Pins the expiry-state transitions in <see cref="TenantService.GetStatusAsync"/> exactly at the
/// ValidUpto and grace-window boundaries, so the Active → InGrace → Expired badges never drift.
/// Grace period is fixed at 7 days for these cases.
/// </summary>
public sealed class TenantServiceStatusBoundaryTests
{
    private const int GraceDays = 7;
    private static readonly DateTime ValidUpto = new(2031, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IMultiTenantStore<AppTenantInfo> _store = Substitute.For<IMultiTenantStore<AppTenantInfo>>();
    private readonly TimeProvider _clock = Substitute.For<TimeProvider>();

    [Theory]
    [InlineData(0, "Active")]                       // exactly at ValidUpto
    [InlineData(1, "InGrace")]                      // 1 second past ValidUpto
    [InlineData(GraceDays * 24 * 3600, "InGrace")]  // exactly at grace end
    [InlineData(GraceDays * 24 * 3600 + 1, "Expired")] // 1 second past grace end
    public async Task GetStatusAsync_Should_ResolveExpiryState_AtBoundary(double offsetSecondsFromValidUpto, string expected)
    {
        const string tenantId = "acme";
        var now = ValidUpto.AddSeconds(offsetSecondsFromValidUpto);
        _clock.GetUtcNow().Returns(new DateTimeOffset(now, TimeSpan.Zero));

        var tenant = new AppTenantInfo(tenantId, "Acme", connectionString: null, adminEmail: "admin@acme.test")
        {
            Plan = "pro",
            ValidUpto = ValidUpto,
        };
        _store.GetAsync(tenantId).Returns(tenant);

        var sut = new TenantService(
            _store,
            Options.Create(new DatabaseOptions { ConnectionString = "Host=localhost;Database=fsh;Username=x;Password=y" }),
            serviceProvider: null!,
            dbContext: null!,
            provisioningService: null!,
            _clock,
            Options.Create(new TenantBillingOptions { GracePeriodDays = GraceDays }),
            NullLogger<TenantService>.Instance);

        var status = await sut.GetStatusAsync(tenantId, CancellationToken.None);

        status.ExpiryState.ShouldBe(expected);
        status.GraceEndsUtc.ShouldBe(ValidUpto.AddDays(GraceDays));
    }
}
