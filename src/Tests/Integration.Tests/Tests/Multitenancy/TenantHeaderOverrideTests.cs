#pragma warning disable S1144 // Unused private members — populated by JSON
#pragma warning disable S3459 // Unassigned members — populated by JSON
using System.Net.Http.Json;
using System.Text.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// Regression coverage for the tenant resolution chain. A root operator must be
/// able to scope a request to another tenant by sending the `tenant` header
/// (e.g. for cross-tenant user search before impersonation). A tenant operator
/// must NOT be able to do the same — that would let them browse other tenants'
/// data simply by setting a header.
///
/// These tests pin the contract enforced by the RootOperatorHeaderOverride
/// delegate strategy in <c>MultitenancyModule</c>.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TenantHeaderOverrideTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    private string _tenantA = default!;
    private string _tenantAAdminEmail = default!;
    private string _tenantB = default!;
    private string _tenantBAdminEmail = default!;

    public TenantHeaderOverrideTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    public async Task InitializeAsync()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        _tenantA = $"thot-a-{unique}";
        _tenantB = $"thot-b-{unique}";
        _tenantAAdminEmail = $"admin-a-{unique}@thot.com";
        _tenantBAdminEmail = $"admin-b-{unique}@thot.com";

        using var rootClient = await _auth.CreateRootAdminClientAsync();
        await CreateTenantAsync(rootClient, _tenantA, _tenantAAdminEmail);
        await CreateTenantAsync(rootClient, _tenantB, _tenantBAdminEmail);
        await WaitForProvisioningAsync(rootClient, _tenantA);
        await WaitForProvisioningAsync(rootClient, _tenantB);

        // Provisioning can report "Completed" a tick before the seeded admin user is queryable; a
        // successful token issuance cross-checks the user exists, avoiding a first-test race on empty lists.
        _ = await GetTokenWithRetryAsync(_tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);
        _ = await GetTokenWithRetryAsync(_tenantBAdminEmail, TestConstants.DefaultPassword, _tenantB);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RootOperator_Should_TargetOtherTenant_When_HeaderProvided()
    {
        // Arrange — root admin is in `root` but sends `tenant: <tenantA>` header
        // to scope this single request to tenant A.
        var rootToken = await _auth.GetRootAdminTokenAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", rootToken.AccessToken);
        client.DefaultRequestHeaders.Add("tenant", _tenantA);

        // Act — search users with no filter. Should return tenant A's admin user.
        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/users/search?PageNumber=1&PageSize=50");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<SearchUserDto>>(Json);
        page.ShouldNotBeNull();
        // Tenant A's admin should be visible.
        page.Items.ShouldContain(u => u.Email == _tenantAAdminEmail);
        // Root-tenant users (admin@root.com) must NOT leak through.
        page.Items.ShouldNotContain(u => u.Email == TestConstants.RootAdminEmail);
        // Tenant B's users must NOT leak through.
        page.Items.ShouldNotContain(u => u.Email == _tenantBAdminEmail);
    }

    [Fact]
    public async Task RootOperator_Should_UseOwnTenant_When_NoHeaderSent()
    {
        // Arrange — bare CreateRootAdminClient sends `tenant: root`; claim matches header, so the
        // override no-ops and resolution scopes to the root tenant.
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await rootClient.GetAsync($"{TestConstants.IdentityBasePath}/users/search?PageNumber=1&PageSize=50");

        // Assert — root admin is in the result set; tenant A / B admins are not.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<SearchUserDto>>(Json);
        page.ShouldNotBeNull();
        page.Items.ShouldContain(u => u.Email == TestConstants.RootAdminEmail);
        page.Items.ShouldNotContain(u => u.Email == _tenantAAdminEmail);
    }

    [Fact]
    public async Task TenantAdmin_HeaderOverride_Should_BeIgnored()
    {
        // Arrange — tenant A's admin sends a `tenant: B` header; the override is gated by claim==root,
        // so it must fail closed and the query stays in tenant A.
        var tenantAToken = await GetTokenWithRetryAsync(_tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tenantAToken.AccessToken);
        client.DefaultRequestHeaders.Add("tenant", _tenantB);

        // Act
        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/users/search?PageNumber=1&PageSize=50");

        // Assert — still resolves to tenant A (claim wins): A's admin present, B's absent. This flips
        // if the override ever leaked to non-root callers.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<SearchUserDto>>(Json);
        page.ShouldNotBeNull();
        page.Items.ShouldContain(u => u.Email == _tenantAAdminEmail);
        page.Items.ShouldNotContain(u => u.Email == _tenantBAdminEmail);
    }

    [Fact]
    public async Task TenantAdmin_Should_AccessOwnTenant_Normally()
    {
        // Arrange — sanity check that the new strategy doesn't break the
        // normal (no-override) path for tenant operators.
        using var tenantAClient = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);

        // Act
        var response = await tenantAClient.GetAsync($"{TestConstants.IdentityBasePath}/users/search?PageNumber=1&PageSize=50");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<SearchUserDto>>(Json);
        page.ShouldNotBeNull();
        page.Items.ShouldContain(u => u.Email == _tenantAAdminEmail);
    }

    // ─── helpers ────────────────────────────────────────────────────────

    private async Task<TokenResult> GetTokenWithRetryAsync(string email, string password, string tenant, int maxRetries = 30)
    {
        Exception? last = null;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _auth.GetTokenAsync(email, password, tenant);
            }
            catch (HttpRequestException ex)
            {
                last = ex;
                await Task.Delay(500);
            }
        }
        throw last ?? new InvalidOperationException("token issuance failed");
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"THOT {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
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

    private sealed class SearchUserDto
    {
        public string? Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
    }
}
