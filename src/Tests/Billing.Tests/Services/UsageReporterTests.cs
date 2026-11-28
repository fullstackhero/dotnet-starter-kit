using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Billing.Tests.Services;

public sealed class UsageReporterTests
{
    private const string TenantId = "tenant-1";
    private const int PeriodYear = 2026;
    private const int PeriodMonth = 10;

    [Fact]
    public async Task CaptureForPeriodAsync_Should_UsePeriodApiForAllResources_AndCurrentValuesForNonPeriodicResources()
    {
        // Arrange
        await using var db = CreateDbContext();
        var (reporter, quotas) = CreateReporter(db, apiCalls: 137);

        // Act
        var snapshots = await reporter.CaptureForPeriodAsync(TenantId, PeriodYear, PeriodMonth);

        // Assert
        snapshots.Single(snapshot => snapshot.Resource == QuotaResource.ApiCalls).UsedUnits.ShouldBe(137);
        quotas.PeriodReads.ShouldBe(Enum.GetValues<QuotaResource>()
            .Select(resource => (resource, PeriodYear, PeriodMonth))
            .ToList());
        quotas.CurrentReads.ShouldBe([
            QuotaResource.StorageBytes,
            QuotaResource.Users,
            QuotaResource.ActiveFeatureFlags
        ]);
    }

    [Fact]
    public async Task CaptureForPeriodAsync_Should_CaptureZeroApiCalls_WhenPeriodCounterIsMissing()
    {
        // Arrange
        await using var db = CreateDbContext();
        var (reporter, _) = CreateReporter(db, apiCalls: 0);

        // Act
        var snapshots = await reporter.CaptureForPeriodAsync(TenantId, PeriodYear, PeriodMonth);

        // Assert
        var apiCalls = snapshots.Single(snapshot => snapshot.Resource == QuotaResource.ApiCalls);
        apiCalls.UsedUnits.ShouldBe(0);
        apiCalls.Overage.ShouldBe(0);
    }

    private static BillingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }

    private static (UsageReporter Reporter, RecordingQuotaService Quotas) CreateReporter(
        BillingDbContext db,
        long apiCalls)
    {
        var tenant = new AppTenantInfo(TenantId, TenantId) { Plan = "free" };
        var tenantStore = Substitute.For<IMultiTenantStore<AppTenantInfo>>();
        tenantStore.GetAsync(TenantId).Returns(Task.FromResult<AppTenantInfo?>(tenant));

        var quotas = new RecordingQuotaService(apiCalls);

        var quotaOptions = new QuotaOptions();
        quotaOptions.Plans["free"] = new Dictionary<QuotaResource, long>
        {
            [QuotaResource.ApiCalls] = 100,
            [QuotaResource.StorageBytes] = 8192,
            [QuotaResource.Users] = 5,
            [QuotaResource.ActiveFeatureFlags] = 10
        };

        var reporter = new UsageReporter(
            db,
            quotas,
            new QuotaPlanResolver(quotaOptions),
            tenantStore,
            NullLogger<UsageReporter>.Instance);

        return (reporter, quotas);
    }

    private sealed class RecordingQuotaService(long apiCalls) : IQuotaService
    {
        public List<QuotaResource> CurrentReads { get; } = [];

        public List<(QuotaResource Resource, int Year, int Month)> PeriodReads { get; } = [];

        public ValueTask<QuotaCheckResult> CheckAsync(
            string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
            => throw new NotSupportedException();

        public ValueTask<long> RecordAsync(
            string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
            => throw new NotSupportedException();

        public ValueTask<QuotaCheckResult> CheckAndRecordAsync(
            string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
            => throw new NotSupportedException();

        public ValueTask<long> GetCurrentAsync(
            string tenantId, QuotaResource resource, CancellationToken ct = default)
        {
            CurrentReads.Add(resource);
            var value = resource switch
            {
                QuotaResource.StorageBytes => 4096L,
                QuotaResource.Users => 3L,
                QuotaResource.ActiveFeatureFlags => 2L,
                _ => 0L
            };
            return ValueTask.FromResult(value);
        }

        public ValueTask<long> GetForPeriodAsync(
            string tenantId, QuotaResource resource, int year, int month, CancellationToken ct = default)
        {
            PeriodReads.Add((resource, year, month));
            return resource == QuotaResource.ApiCalls
                ? ValueTask.FromResult(apiCalls)
                : GetCurrentAsync(tenantId, resource, ct);
        }
    }
}
