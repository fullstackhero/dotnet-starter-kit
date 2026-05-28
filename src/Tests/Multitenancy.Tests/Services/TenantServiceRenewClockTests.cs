using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Multitenancy;
using FSH.Modules.Multitenancy.Provisioning;
using FSH.Modules.Multitenancy.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Multitenancy.Tests.Services;

/// <summary>
/// Guards that <see cref="TenantService.RenewAsync"/> derives "now" from the injected
/// <see cref="TimeProvider"/> (not <c>DateTime.UtcNow</c>), so the renewal stacking math is
/// clock-controllable and consistent with the rest of the service.
/// </summary>
public sealed class TenantServiceRenewClockTests
{
    private readonly IMultiTenantStore<AppTenantInfo> _store = Substitute.For<IMultiTenantStore<AppTenantInfo>>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly TimeProvider _clock = Substitute.For<TimeProvider>();

    private TenantService CreateSut()
    {
        // RefreshTenantCacheAsync resolves IEnumerable<IMultiTenantStore<...>>; return empty so it no-ops.
        _serviceProvider
            .GetService(typeof(IEnumerable<IMultiTenantStore<AppTenantInfo>>))
            .Returns(Array.Empty<IMultiTenantStore<AppTenantInfo>>());

        return new TenantService(
            _store,
            Options.Create(new DatabaseOptions { ConnectionString = "Host=localhost;Database=fsh;Username=x;Password=y" }),
            _serviceProvider,
            dbContext: null!,            // RenewAsync never touches the DbContext
            provisioningService: null!,  // RenewAsync never touches provisioning
            _clock,
            Options.Create(new TenantBillingOptions()),
            NullLogger<TenantService>.Instance);
    }

    [Fact]
    public async Task RenewAsync_Should_DeriveNow_FromInjectedTimeProvider_When_ValidityIsInThePast()
    {
        // Arrange: a fixed fake "now" far from the real wall clock so a DateTime.UtcNow regression diverges.
        var fakeNow = new DateTimeOffset(2031, 3, 15, 0, 0, 0, TimeSpan.Zero);
        _clock.GetUtcNow().Returns(fakeNow);

        const string tenantId = "acme";
        var tenant = new AppTenantInfo(tenantId, "Acme", connectionString: null, adminEmail: "admin@acme.test")
        {
            Plan = "pro",
            // Past relative to both the real clock and the fake clock, so periodStart must equal "now".
            ValidUpto = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        _store.GetAsync(tenantId).Returns(tenant);

        var sut = CreateSut();

        // Act: renew one monthly term.
        var (periodStart, validUpto, planChanged) = await sut.RenewAsync(tenantId, "pro", termMonths: 1, CancellationToken.None);

        // Assert: stacking starts from the injected clock, not the system clock.
        periodStart.ShouldBe(fakeNow.UtcDateTime);
        validUpto.ShouldBe(fakeNow.UtcDateTime.AddMonths(1));
        planChanged.ShouldBeFalse();
    }
}
