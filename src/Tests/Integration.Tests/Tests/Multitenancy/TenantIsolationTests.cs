using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

[Collection(FshCollectionDefinition.Name)]
public sealed class TenantIsolationTests
{
    private readonly AuthHelper _auth;

    public TenantIsolationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task TenantIsolation_Should_NotLeakUsers_When_QueryingAcrossTenants()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Create and provision a new tenant
        var tenantId = $"iso-{uniqueId}";
        var tenantAdminEmail = $"isoadmin-{uniqueId}@tenant.com";
        await CreateTenantAsync(rootClient, tenantId, tenantAdminEmail);
        await WaitForProvisioningAsync(rootClient, tenantId);

        // Login as new tenant admin — retry because provisioning may still be finalizing
        using var tenantClient = await CreateTenantAdminClientWithRetryAsync(
            tenantAdminEmail, TestConstants.DefaultPassword, tenantId);

        var registerResponse = await tenantClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/register", new
            {
                firstName = "Isolated",
                lastName = "User",
                email = $"isolated-{uniqueId}@example.com",
                userName = $"isolateduser-{uniqueId}",
                password = "Test@1234!",
                confirmPassword = "Test@1234!"
            });
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Query users from root tenant — isolated user must NOT appear
        var rootUsersResponse = await rootClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/search?pageNumber=1&pageSize=100");
        rootUsersResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var rootUsersContent = await rootUsersResponse.Content.ReadAsStringAsync();
        rootUsersContent.ShouldNotContain($"isolated-{uniqueId}@example.com");
    }

    [Fact]
    public async Task TenantIsolation_Should_AllowSameEmailInDifferentTenants()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        string sharedEmail = $"shared-{uniqueId}@example.com";

        // Create two tenants
        var tenant1Id = $"dup1-{uniqueId}";
        var tenant2Id = $"dup2-{uniqueId}";
        var t1AdminEmail = $"t1admin-{uniqueId}@tenant.com";
        var t2AdminEmail = $"t2admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, tenant1Id, t1AdminEmail);
        await CreateTenantAsync(rootClient, tenant2Id, t2AdminEmail);
        await WaitForProvisioningAsync(rootClient, tenant1Id);
        await WaitForProvisioningAsync(rootClient, tenant2Id);

        // Register same email in tenant 1
        using var client1 = await CreateTenantAdminClientWithRetryAsync(
            t1AdminEmail, TestConstants.DefaultPassword, tenant1Id);
        var register1 = await client1.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "Shared", lastName = "Email", email = sharedEmail,
            userName = $"shared1-{uniqueId}", password = "Test@1234!", confirmPassword = "Test@1234!"
        });
        register1.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Register same email in tenant 2 — should succeed
        using var client2 = await CreateTenantAdminClientWithRetryAsync(
            t2AdminEmail, TestConstants.DefaultPassword, tenant2Id);
        var register2 = await client2.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "Shared", lastName = "Email", email = sharedEmail,
            userName = $"shared2-{uniqueId}", password = "Test@1234!", confirmPassword = "Test@1234!"
        });

        register2.StatusCode.ShouldBe(HttpStatusCode.Created);
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

        // Get final status for the error message
        var finalResponse = await client.GetAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
        var finalContent = finalResponse.IsSuccessStatusCode
            ? await finalResponse.Content.ReadAsStringAsync()
            : $"HTTP {finalResponse.StatusCode}";

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds. Last status: {finalContent}");
    }
}
