#pragma warning disable S1144 // Unused private members — populated by JSON deserialization
#pragma warning disable S3459 // Unassigned members — populated by JSON deserialization
using System.Text.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// Coverage for the operator validity override (<c>POST /api/v1/tenants/{id}/adjust-validity</c>):
/// sets <c>ValidUpto</c> to an explicit date with no billing side-effect (no new invoice / subscription),
/// allows backdating (immediate expiry) unlike renewal, and is root-only.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class AdjustTenantValidityTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public AdjustTenantValidityTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task AdjustValidity_Should_Set_ExplicitFutureDate_WithoutBillingSideEffect()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"adj-{unique}";
        var planKey = await CreatePlanAsync(rootClient, $"adj-m-{unique}", monthlyBasePrice: 10m);
        await CreateTenantAsync(rootClient, tenantId, $"adj-{unique}@tenant.com", planKey);

        // The subscription invoice is created synchronously on tenant create; capture the count first.
        var invoicesBefore = await GetInvoiceCountAsync(rootClient, tenantId);

        var target = DateTime.UtcNow.AddYears(2);
        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/adjust-validity",
            new { tenantId, validUpto = target });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<AdjustResult>(Json);
        result.ShouldNotBeNull();
        result.ValidUpto.ShouldBe(target, tolerance: TimeSpan.FromSeconds(1));

        var status = await GetStatusAsync(rootClient, tenantId);
        status.ValidUpto!.Value.ShouldBe(target, tolerance: TimeSpan.FromSeconds(1));
        status.ExpiryState.ShouldBe("Active");

        var invoicesAfter = await GetInvoiceCountAsync(rootClient, tenantId);
        invoicesAfter.ShouldBe(invoicesBefore, "adjust-validity must not create an invoice");
    }

    [Fact]
    public async Task AdjustValidity_Should_Allow_Backdating_ToExpireImmediately()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"adj-back-{unique}";
        var planKey = await CreatePlanAsync(rootClient, $"adj-b-{unique}", monthlyBasePrice: 10m);
        await CreateTenantAsync(rootClient, tenantId, $"adj-back-{unique}@tenant.com", planKey);

        // Backdate well past the grace window — renewal would reject this; the override allows it.
        var target = DateTime.UtcNow.AddDays(-30);
        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/adjust-validity",
            new { tenantId, validUpto = target });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var status = await GetStatusAsync(rootClient, tenantId);
        status.ValidUpto!.Value.ShouldBe(target, tolerance: TimeSpan.FromSeconds(1));
        status.ExpiryState.ShouldBe("Expired", "a tenant backdated past grace is expired");
    }

    [Fact]
    public async Task AdjustValidity_Should_Return400_When_RouteIdDoesNotMatchBody()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"adj-mm-{unique}";
        var planKey = await CreatePlanAsync(rootClient, $"adj-mm-{unique}", monthlyBasePrice: 5m);
        await CreateTenantAsync(rootClient, tenantId, $"adj-mm-{unique}@tenant.com", planKey);

        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/adjust-validity",
            new { tenantId = "some-other-tenant", validUpto = DateTime.UtcNow.AddMonths(1) });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AdjustValidity_Should_Return400_When_TargetIsRootTenant()
    {
        // The root operator tenant must never expire — adjusting its validity is rejected so an
        // operator can't accidentally backdate the platform tenant into an expired state.
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        var response = await rootClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{TestConstants.RootTenantId}/adjust-validity",
            new { tenantId = TestConstants.RootTenantId, validUpto = DateTime.UtcNow.AddDays(-1) });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest,
            "the root operator tenant's validity must not be adjustable");
    }

    [Fact]
    public async Task AdjustValidity_Should_Return401_When_NotAuthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/anytenant/adjust-validity",
            new { tenantId = "anytenant", validUpto = DateTime.UtcNow.AddMonths(1) });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdjustValidity_Should_Forbid_When_CallerIsTenantAdmin()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"adj-authz-{unique}";
        var adminEmail = $"adj-authz-{unique}@tenant.com";
        var planKey = await CreatePlanAsync(rootClient, $"adj-az-{unique}", monthlyBasePrice: 5m);
        await CreateTenantAsync(rootClient, tenantId, adminEmail, planKey);
        await WaitForProvisioningAsync(rootClient, tenantId);

        using var tenantClient = await CreateTenantAdminClientWithRetryAsync(
            adminEmail, TestConstants.DefaultPassword, tenantId);

        var response = await tenantClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/adjust-validity",
            new { tenantId, validUpto = DateTime.UtcNow.AddMonths(1) });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

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

    private static async Task<string> CreatePlanAsync(HttpClient client, string key, decimal monthlyBasePrice)
    {
        var resp = await client.PostAsJsonAsync($"{BillingBasePath}/plans",
            new { key, name = $"Plan {key}", currency = "USD", monthlyBasePrice });
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
        return key;
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail, string planKey)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Adjust {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
            planKey,
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task<long> GetInvoiceCountAsync(HttpClient client, string tenantId)
    {
        var resp = await client.GetAsync($"{BillingBasePath}/invoices?tenantId={tenantId}&pageNumber=1&pageSize=50");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("totalCount").GetInt64();
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

    private sealed record AdjustResult(string TenantId, DateTime ValidUpto);

    private sealed record TenantStatus
    {
        public string Id { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime? ValidUpto { get; init; }
        public string? Plan { get; init; }
        public string ExpiryState { get; init; } = string.Empty;
    }

    #endregion
}
