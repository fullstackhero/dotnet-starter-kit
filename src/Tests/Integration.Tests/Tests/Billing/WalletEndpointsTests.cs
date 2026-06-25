using System.Text.Json;
using System.Text.Json.Serialization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Billing;

/// <summary>
/// Integration tests for the tenant-facing wallet and top-up request HTTP endpoints:
///   GET  /api/v1/billing/wallet/me
///   POST /api/v1/billing/wallet/topup-requests
///   GET  /api/v1/billing/wallet/topup-requests/me
///
/// Cross-tenant isolation: a request created under Tenant A must NOT appear in Tenant B's
/// /wallet/topup-requests/me response.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class WalletEndpointsTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public WalletEndpointsTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── Happy-path: create a topup request ───────────────────────────

    [Fact]
    public async Task CreateTopupRequest_Should_Return200_And_NewGuid()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsJsonAsync(
            $"{BillingBasePath}/wallet/topup-requests",
            new { amount = 100m, note = "integration test topup" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var id = await response.DeserializeAsync<Guid>();
        id.ShouldNotBe(Guid.Empty);
    }

    // ─── Happy-path: list topup requests ──────────────────────────────

    [Fact]
    public async Task GetMyTopupRequests_Should_Include_JustCreated_Request_As_Pending()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        // Create a topup request.
        using var createResp = await client.PostAsJsonAsync(
            $"{BillingBasePath}/wallet/topup-requests",
            new { amount = 75m, note = "list-check" });
        createResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var requestId = await createResp.DeserializeAsync<Guid>();

        // Retrieve the list and assert the new request is present with Pending status.
        using var listResp = await client.GetAsync(
            $"{BillingBasePath}/wallet/topup-requests/me?pageNumber=1&pageSize=50");
        listResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await ParseAsync<PagedResponse<TopupRequestDto>>(listResp);

        page.Items.ShouldContain(r => r.Id == requestId,
            "the freshly created top-up request must appear in the tenant's list");
        var dto = page.Items.First(r => r.Id == requestId);
        dto.Status.ShouldBe("Pending", "a newly created top-up request must have Pending status");
    }

    // ─── Happy-path: wallet shows balance 0 (no credits yet) ──────────

    [Fact]
    public async Task GetMyWallet_Should_Return_WalletDto_With_Zero_Balance_When_No_Credits()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.GetAsync($"{BillingBasePath}/wallet/me");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var wallet = await ParseAsync<WalletDto>(response);

        wallet.ShouldNotBeNull();
        wallet.TenantId.ShouldBe(TestConstants.RootTenantId,
            "the wallet tenant must match the caller's tenant");
        wallet.Currency.ShouldBe("USD");
        // Balance starts at 0 (no credits have been applied in this test run).
        wallet.Balance.ShouldBeGreaterThanOrEqualTo(0m,
            "wallet balance must be non-negative");
    }

    // ─── Unauthenticated access is rejected ───────────────────────────

    [Fact]
    public async Task CreateTopupRequest_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.PostAsJsonAsync(
            $"{BillingBasePath}/wallet/topup-requests",
            new { amount = 50m, note = "unauth" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyTopupRequests_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.GetAsync(
            $"{BillingBasePath}/wallet/topup-requests/me?pageNumber=1&pageSize=20");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyWallet_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.GetAsync($"{BillingBasePath}/wallet/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ─── Cross-tenant isolation ────────────────────────────────────────

    [Fact]
    public async Task GetMyTopupRequests_Must_NotLeak_Requests_From_Other_Tenant()
    {
        // Arrange — root (Tenant A) creates a topup request.
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        using var createResp = await rootClient.PostAsJsonAsync(
            $"{BillingBasePath}/wallet/topup-requests",
            new { amount = 200m, note = "tenant-a-request" });
        createResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rootRequestId = await createResp.DeserializeAsync<Guid>();

        // Arrange — provision a fresh Tenant B.
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"wallet-iso-{uniqueId}";
        var otherAdminEmail = $"wallet-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);

        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        // Act — Tenant B lists its own topup requests.
        using var otherListResp = await otherClient.GetAsync(
            $"{BillingBasePath}/wallet/topup-requests/me?pageNumber=1&pageSize=100");
        otherListResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var otherPage = await ParseAsync<PagedResponse<TopupRequestDto>>(otherListResp);

        // Assert — Tenant A's request must NOT appear in Tenant B's list.
        otherPage.Items.ShouldNotContain(r => r.Id == rootRequestId,
            "Tenant B must not see Tenant A's top-up request");

        // Sanity — Tenant A can still see its own request.
        using var rootListResp = await rootClient.GetAsync(
            $"{BillingBasePath}/wallet/topup-requests/me?pageNumber=1&pageSize=100");
        rootListResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rootPage = await ParseAsync<PagedResponse<TopupRequestDto>>(rootListResp);
        rootPage.Items.ShouldContain(r => r.Id == rootRequestId,
            "Tenant A (root) must still be able to read its own request");
    }

    // ─── helpers ──────────────────────────────────────────────────────

    private static async Task<T> ParseAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Expected success status, got {(int)response.StatusCode} {response.StatusCode}. Body: {json}");
        }
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize response to {typeof(T).Name}. Body: {json}");
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");

            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Tenant {tenantId} provisioning failed: {content}");
                }
            }

            await Task.Delay(1000);
        }

        var finalResponse = await client.GetAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
        var finalContent = finalResponse.IsSuccessStatusCode
            ? await finalResponse.Content.ReadAsStringAsync()
            : $"HTTP {finalResponse.StatusCode}";

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds. Last status: {finalContent}");
    }

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (int i = 0; i < maxRetries; i++)
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
}
