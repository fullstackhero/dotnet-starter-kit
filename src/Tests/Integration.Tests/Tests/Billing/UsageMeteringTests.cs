using System.Text.Json;
using FSH.Modules.Billing.Contracts.Dtos;
using Hangfire;
using Hangfire.Storage;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Billing;

[Collection(FshCollectionDefinition.Name)]
public sealed class UsageMeteringTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public UsageMeteringTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task CaptureUsageSnapshots_Should_ReturnOneSnapshotPerQuotaResource_When_FirstCalled()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (year, month) = (2030, (DateTime.UtcNow.Month % 12) + 1);

        var response = await client.PostAsJsonAsync(
            $"{BillingBasePath}/usage/snapshots/capture",
            new { tenantId = TestConstants.RootTenantId, periodYear = year, periodMonth = month });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var snapshots = await DeserializeAsync(response);
        snapshots.ShouldNotBeNull();
        snapshots.Count.ShouldBeGreaterThan(0);
        snapshots.ShouldAllBe(s => s.TenantId == TestConstants.RootTenantId);
        snapshots.ShouldAllBe(s => s.PeriodYear == year && s.PeriodMonth == month);

        // One snapshot per QuotaResource enum value (currently 4: ApiCalls, StorageBytes, Users, ActiveFeatureFlags)
        snapshots.Select(s => s.Resource).Distinct().Count().ShouldBe(snapshots.Count);
    }

    [Fact]
    public async Task CaptureUsageSnapshots_Should_BeIdempotent_When_CalledTwiceForSamePeriod()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (year, month) = (2031, 3);

        var firstResponse = await client.PostAsJsonAsync(
            $"{BillingBasePath}/usage/snapshots/capture",
            new { tenantId = TestConstants.RootTenantId, periodYear = year, periodMonth = month });
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var first = await DeserializeAsync(firstResponse);

        var secondResponse = await client.PostAsJsonAsync(
            $"{BillingBasePath}/usage/snapshots/capture",
            new { tenantId = TestConstants.RootTenantId, periodYear = year, periodMonth = month });
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var second = await DeserializeAsync(secondResponse);

        first.Count.ShouldBe(second.Count);
        var firstIds = first.Select(s => s.Id).OrderBy(id => id).ToList();
        var secondIds = second.Select(s => s.Id).OrderBy(id => id).ToList();
        secondIds.ShouldBe(firstIds, "Re-running capture for the same period must return the same snapshot Ids.");
    }

    [Fact]
    public async Task CaptureUsageSnapshots_Should_Return401_When_NotAuthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.PostAsJsonAsync(
            $"{BillingBasePath}/usage/snapshots/capture",
            new { tenantId = TestConstants.RootTenantId, periodYear = 2030, periodMonth = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CaptureUsageSnapshots_Should_RejectInvalidPeriod()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            $"{BillingBasePath}/usage/snapshots/capture",
            new { tenantId = TestConstants.RootTenantId, periodYear = 1999, periodMonth = 13 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void MonthlyInvoiceJob_Should_BeRegisteredAsRecurringJob()
    {
        // Touch the server so module endpoint mapping (which registers the job) runs
        _ = _factory.Server;

        var connection = JobStorage.Current.GetConnection();
        var recurringJobs = connection.GetRecurringJobs();

        var billingJob = recurringJobs.FirstOrDefault(j => j.Id == "billing-monthly-invoices");
        billingJob.ShouldNotBeNull("Recurring job 'billing-monthly-invoices' should be registered by BillingModule.");
        billingJob.Cron.ShouldBe("5 0 1 * *");
    }

    private static async Task<List<UsageSnapshotDto>> DeserializeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<UsageSnapshotDto>>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize snapshot list.");
    }
}
