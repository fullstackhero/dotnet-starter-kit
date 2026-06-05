#pragma warning disable S1144 // Unused private members — populated by JSON deserialization
#pragma warning disable S3459 // Unassigned members — populated by JSON deserialization
using System.Text.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// Coverage for the plan-driven tenant renewal endpoint (<c>POST /api/v1/tenants/{id}/renew</c>):
/// extends validity by one plan term (stacking on remaining time), switches plan when a different
/// key is supplied, route/body mismatch and empty-tenant validation, and root-only authorization.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class RenewTenantTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public RenewTenantTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task RenewTenant_Should_Extend_Validity_By_One_Term()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"renew-{unique}";
        var planKey = await CreatePlanAsync(rootClient, $"renew-m-{unique}", monthlyBasePrice: 10m);
        await CreateTenantAsync(rootClient, tenantId, $"renew-{unique}@tenant.com", planKey);

        var before = (await GetStatusAsync(rootClient, tenantId)).ValidUpto!.Value;

        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/renew",
            new { tenantId, planKey });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RenewResult>(Json);
        result.ShouldNotBeNull();
        result.PlanChanged.ShouldBeFalse("renewing the same plan does not change it");
        // Monthly plan → validity advances ~1 month from the prior ValidUpto (stacking).
        result.ValidUpto.ShouldBeGreaterThan(before.AddDays(27));
        result.ValidUpto.ShouldBeLessThan(before.AddDays(32));

        var after = (await GetStatusAsync(rootClient, tenantId)).ValidUpto!.Value;
        after.ShouldBe(result.ValidUpto, tolerance: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RenewTenant_Should_Switch_Plan_When_Different_Key_Supplied()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"renew-sw-{unique}";
        var monthly = await CreatePlanAsync(rootClient, $"sw-m-{unique}", monthlyBasePrice: 10m);
        var annual = await CreateYearlyPlanAsync(rootClient, $"sw-y-{unique}", annualPrice: 100m);
        await CreateTenantAsync(rootClient, tenantId, $"renew-sw-{unique}@tenant.com", monthly);

        var before = (await GetStatusAsync(rootClient, tenantId)).ValidUpto!.Value;

        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/renew",
            new { tenantId, planKey = annual });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RenewResult>(Json);
        result.ShouldNotBeNull();
        result.PlanChanged.ShouldBeTrue("switching from monthly to annual changes the plan");
        result.PlanKey.ShouldBe(annual);
        // Yearly term → ~12 months from the prior validity.
        result.ValidUpto.ShouldBeGreaterThan(before.AddDays(360));

        var status = await GetStatusAsync(rootClient, tenantId);
        status.Plan.ShouldBe(annual);
    }

    [Fact]
    public async Task RenewTenant_Should_Stack_Two_Terms_When_RenewedTwice()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"renew-x2-{unique}";
        var planKey = await CreatePlanAsync(rootClient, $"x2-m-{unique}", monthlyBasePrice: 10m);
        await CreateTenantAsync(rootClient, tenantId, $"renew-x2-{unique}@tenant.com", planKey);

        var before = (await GetStatusAsync(rootClient, tenantId)).ValidUpto!.Value;

        await RenewAsync(rootClient, tenantId, planKey);
        await RenewAsync(rootClient, tenantId, planKey);

        var after = (await GetStatusAsync(rootClient, tenantId)).ValidUpto!.Value;
        // Two monthly terms stacked onto the validity present before the renewals.
        after.ShouldBeGreaterThan(before.AddDays(58));
        after.ShouldBeLessThan(before.AddDays(64));
    }

    [Fact]
    public async Task RenewTenant_Should_StartFromNow_When_TenantHasLapsed()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"renew-lapsed-{unique}";
        var planKey = await CreatePlanAsync(rootClient, $"lap-m-{unique}", monthlyBasePrice: 10m);
        await CreateTenantAsync(rootClient, tenantId, $"renew-lapsed-{unique}@tenant.com", planKey);

        // Lapse the tenant 30 days ago (operator override), then renew: stacking must restart from "now",
        // not from the long-past validity, so the tenant gets a full term going forward.
        var adjust = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/adjust-validity",
            new { tenantId, validUpto = DateTime.UtcNow.AddDays(-30) });
        adjust.StatusCode.ShouldBe(HttpStatusCode.OK, await adjust.Content.ReadAsStringAsync());

        var result = await RenewAsync(rootClient, tenantId, planKey);

        result.ValidUpto.ShouldBeGreaterThan(DateTime.UtcNow.AddDays(27),
            "renewing a lapsed tenant must restart the term from now, not stack on the past validity");
        result.ValidUpto.ShouldBeLessThan(DateTime.UtcNow.AddDays(32));
    }

    [Fact]
    public async Task RenewTenant_Should_Advance_SubscriptionEndUtc_On_SamePlanRenewal()
    {
        // Regression for billing/tenant drift: a same-plan renewal advanced tenant.ValidUpto but left
        // Subscription.EndUtc untouched, so the two diverged each renewal. Both must move together.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"renew-drift-{unique}";
        var planKey = await CreatePlanAsync(rootClient, $"drift-m-{unique}", monthlyBasePrice: 10m);
        await CreateTenantAsync(rootClient, tenantId, $"renew-drift-{unique}@tenant.com", planKey);
        await WaitForProvisioningAsync(rootClient, tenantId);

        var before = await GetSubscriptionEndUtcAsync(rootClient, tenantId);
        before.ShouldNotBeNull("a plan-bound tenant has an active subscription with an end date");

        var result = await RenewAsync(rootClient, tenantId, planKey);
        result.PlanChanged.ShouldBeFalse("renewing the same plan does not change it");

        var after = await PollSubscriptionEndUtcAdvancedAsync(rootClient, tenantId, before!.Value);
        after.ShouldNotBeNull();
        after!.Value.ShouldBeGreaterThan(before.Value,
            "a same-plan renewal must extend Subscription.EndUtc, not just tenant ValidUpto");
        after.Value.ShouldBe(result.ValidUpto, tolerance: TimeSpan.FromSeconds(5),
            "the subscription term should track the renewed validity");
    }

    #endregion

    #region Validation / Bad Request

    [Fact]
    public async Task RenewTenant_Should_Return400_When_RouteIdDoesNotMatchBody()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"renew-mm-{unique}";
        var planKey = await CreatePlanAsync(rootClient, $"mm-{unique}", monthlyBasePrice: 5m);
        await CreateTenantAsync(rootClient, tenantId, $"renew-mm-{unique}@tenant.com", planKey);

        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/renew",
            new { tenantId = "some-other-tenant", planKey });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RenewTenant_Should_Return400_When_TenantIsEmpty()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"renew-empty-{unique}";
        var planKey = await CreatePlanAsync(rootClient, $"em-{unique}", monthlyBasePrice: 5m);
        await CreateTenantAsync(rootClient, tenantId, $"renew-empty-{unique}@tenant.com", planKey);

        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/renew",
            new { tenantId = "", planKey });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region AuthZ

    [Fact]
    public async Task RenewTenant_Should_Return401_When_NotAuthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/anytenant/renew",
            new { tenantId = "anytenant", planKey = "pro" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RenewTenant_Should_Forbid_When_CallerIsTenantAdmin()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"renew-authz-{unique}";
        var adminEmail = $"renew-authz-{unique}@tenant.com";
        var planKey = await CreatePlanAsync(rootClient, $"az-{unique}", monthlyBasePrice: 5m);
        await CreateTenantAsync(rootClient, tenantId, adminEmail, planKey);
        await WaitForProvisioningAsync(rootClient, tenantId);

        using var tenantClient = await CreateTenantAdminClientWithRetryAsync(
            adminEmail, TestConstants.DefaultPassword, tenantId);

        var response = await tenantClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/renew",
            new { tenantId, planKey });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Helpers

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
            }
            catch (HttpRequestException) when (i < maxRetries - 1)
            {
                await Task.Delay(1000);
            }
        }
        return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
    }

    private static async Task<RenewResult> RenewAsync(HttpClient client, string tenantId, string planKey)
    {
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/renew",
            new { tenantId, planKey });
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<RenewResult>(Json);
        result.ShouldNotBeNull();
        return result!;
    }

    private static async Task<string> CreatePlanAsync(HttpClient client, string key, decimal monthlyBasePrice)
    {
        var resp = await client.PostAsJsonAsync($"{BillingBasePath}/plans",
            new { key, name = $"Plan {key}", currency = "USD", monthlyBasePrice });
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
        return key;
    }

    private static async Task<string> CreateYearlyPlanAsync(HttpClient client, string key, decimal annualPrice)
    {
        var resp = await client.PostAsJsonAsync($"{BillingBasePath}/plans",
            new { key, name = $"Plan {key}", currency = "USD", monthlyBasePrice = 0m, interval = 1, annualPrice });
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
        return key;
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail, string planKey)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Renew {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
            planKey,
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task<TenantStatus> GetStatusAsync(HttpClient client, string tenantId)
    {
        var resp = await client.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/status");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var status = await resp.Content.ReadFromJsonAsync<TenantStatus>(Json);
        status.ShouldNotBeNull();
        return status!;
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Tenant {tenantId} provisioning failed: {content}");
                }
            }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"Tenant {tenantId} did not finish provisioning.");
    }

    private static async Task<DateTime?> GetSubscriptionEndUtcAsync(HttpClient client, string tenantId)
    {
        var resp = await client.GetAsync($"{BillingBasePath}/subscriptions?tenantId={tenantId}");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await resp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return null;
        }
        return JsonSerializer.Deserialize<SubRow>(json, Json)?.EndUtc;
    }

    // The subscription extension is applied by the renewal integration handler; allow a brief window
    // in case dispatch is not perfectly synchronous with the renew response.
    private static async Task<DateTime?> PollSubscriptionEndUtcAdvancedAsync(
        HttpClient client, string tenantId, DateTime baseline, int maxRetries = 20)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            var end = await GetSubscriptionEndUtcAsync(client, tenantId);
            if (end is { } e && e > baseline)
            {
                return end;
            }
            await Task.Delay(500);
        }
        return await GetSubscriptionEndUtcAsync(client, tenantId);
    }

    private sealed record SubRow
    {
        public DateTime? EndUtc { get; init; }
    }

    private sealed record RenewResult(string TenantId, DateTime ValidUpto, string PlanKey, bool PlanChanged);

    private sealed record TenantStatus
    {
        public string Id { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime? ValidUpto { get; init; }
        public string? Plan { get; init; }
    }

    #endregion
}
