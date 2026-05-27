using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Auditing;

[Collection(FshCollectionDefinition.Name)]
public sealed class AuditTenantIsolationTests
{
    private readonly AuthHelper _auth;

    public AuditTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetAudits_Should_OnlyReturnCurrentTenantsRecords()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"audit-iso-{uniqueId}";
        var otherAdminEmail = $"audit-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);

        // Authenticating as both tenants triggers audit events on both sides.
        _ = await _auth.GetRootAdminTokenAsync();
        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        // Wait for audit flush + retry until otherClient sees its own audits.
        string body = string.Empty;
        for (int i = 0; i < 20; i++)
        {
            var response = await otherClient.GetAsync(
                $"{TestConstants.AuditsBasePath}?pageNumber=1&pageSize=100");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            body = await response.Content.ReadAsStringAsync();

            if (body.Contains($"\"tenantId\":\"{otherTenantId}\"", StringComparison.Ordinal))
            {
                break;
            }

            await Task.Delay(500);
        }

        body.ShouldContain($"\"tenantId\":\"{otherTenantId}\"");
        body.ShouldNotContain($"\"tenantId\":\"{TestConstants.RootTenantId}\"");
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

        var finalResponse = await client.GetAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
        var finalContent = finalResponse.IsSuccessStatusCode
            ? await finalResponse.Content.ReadAsStringAsync()
            : $"HTTP {finalResponse.StatusCode}";

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds. Last status: {finalContent}");
    }
}
