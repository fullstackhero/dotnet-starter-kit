#pragma warning disable S1144, S3459
using System.Text.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// Coverage for the tenant-self status endpoint (<c>GET /api/v1/tenants/me/status</c>) that powers the
/// dashboard's plan view + expiry banner: an authenticated tenant gets its own plan/validity/expiry
/// state resolved from the caller context; an unauthenticated request is rejected.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class MyTenantStatusTests
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public MyTenantStatusTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetMyStatus_Should_Return_CallersOwn_Plan_And_ExpiryState()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"mystatus-{unique}";
        var adminEmail = $"mystatus-{unique}@tenant.com";
        var planKey = await CreatePlanAsync(rootClient, $"mystatus-m-{unique}", 10m);
        await CreateTenantAsync(rootClient, tenantId, adminEmail, planKey);
        await WaitForProvisioningAsync(rootClient, tenantId);

        using var tenantClient = await CreateTenantAdminClientWithRetryAsync(adminEmail, TestConstants.DefaultPassword, tenantId);

        var resp = await tenantClient.GetAsync($"{TestConstants.TenantsBasePath}/me/status");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());

        var status = await resp.Content.ReadFromJsonAsync<MyStatus>(Json);
        status.ShouldNotBeNull();
        status!.Id.ShouldBe(tenantId);
        status.Plan.ShouldBe(planKey);
        status.ExpiryState.ShouldBe("Active");
    }

    [Fact]
    public async Task GetMyStatus_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var resp = await client.GetAsync($"{TestConstants.TenantsBasePath}/me/status");

        resp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

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

    private static async Task<string> CreatePlanAsync(HttpClient client, string key, decimal monthlyBasePrice)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/billing/plans",
            new { key, name = $"Plan {key}", currency = "USD", monthlyBasePrice });
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
        return key;
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail, string planKey)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"MyStatus {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
            planKey,
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
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

    private sealed record MyStatus
    {
        public string Id { get; init; } = string.Empty;
        public string? Plan { get; init; }
        public string ExpiryState { get; init; } = string.Empty;
    }
}
