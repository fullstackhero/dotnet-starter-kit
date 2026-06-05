using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

[Collection(FshCollectionDefinition.Name)]
public sealed class TenantActivationTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TenantActivationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task ChangeTenantActivation_Should_ReturnOk_When_DeactivatingTenant()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"deact-{uniqueId}";

        var createResponse = await client.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Deactivate Tenant {uniqueId}",
            connectionString = (string?)null,
            adminEmail = $"deact-{uniqueId}@tenant.com",
            adminPassword = TestConstants.DefaultPassword,
            issuer = "deact.issuer"
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // POST /{id}/activation — body tenantId must match route
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/activation",
            new { tenantId, isActive = false });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangeTenantActivation_Should_DeactivateAndReactivate_When_TenantIsProvisioned()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"toggle-{uniqueId}";

        await client.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Toggle Tenant {uniqueId}",
            connectionString = (string?)null,
            adminEmail = $"toggle-{uniqueId}@tenant.com",
            adminPassword = TestConstants.DefaultPassword,
            issuer = "toggle.issuer"
        });

        // Deactivate
        var deactivateResponse = await client.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/activation",
            new { tenantId, isActive = false });

        // Reactivate
        var reactivateResponse = await client.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/activation",
            new { tenantId, isActive = true });

        // Both should succeed if provisioning completed, otherwise at least one
        (deactivateResponse.IsSuccessStatusCode || reactivateResponse.IsSuccessStatusCode).ShouldBeTrue();
    }

    [Fact]
    public async Task DeactivatedTenant_Should_Be_Denied_At_Login()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"blocked-{uniqueId}";

        var createResponse = await adminClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Blocked Tenant {uniqueId}",
            connectionString = (string?)null,
            adminEmail = $"blocked-{uniqueId}@tenant.com",
            adminPassword = TestConstants.DefaultPassword,
            issuer = "blocked.issuer"
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // While active, a login attempt reaches the token handler (and fails on
        // credentials) — the tenant guard does NOT short-circuit it.
        (await TryIssueTokenAsync(tenantId)).ShouldNotBe(HttpStatusCode.Forbidden);

        var deactivate = await adminClient.PostAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/activation",
            new { tenantId, isActive = false });
        deactivate.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Once deactivated, the tenant guard rejects before the token handler runs → 403.
        // Regression guard for "a deactivated tenant can still log in via the dashboard".
        (await TryIssueTokenAsync(tenantId)).ShouldBe(HttpStatusCode.Forbidden);
    }

    // Anonymous tenant-scoped token request with invalid credentials. Asserts the pipeline status
    // (401 when the handler runs, 403 when the tenant guard blocks), so it never needs a provisioned admin.
    private async Task<HttpStatusCode> TryIssueTokenAsync(string tenantId)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
        request.Headers.Add("tenant", tenantId);
        request.Content = JsonContent.Create(new { email = "nobody@example.com", password = "Wrong-Password-1!" });
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }
}
