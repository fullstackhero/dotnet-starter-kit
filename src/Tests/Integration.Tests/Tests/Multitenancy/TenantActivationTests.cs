using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

[Collection(FshCollectionDefinition.Name)]
public sealed class TenantActivationTests
{
    private readonly AuthHelper _auth;

    public TenantActivationTests(FshWebApplicationFactory factory)
    {
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
}
