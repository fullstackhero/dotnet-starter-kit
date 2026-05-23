using FSH.Modules.Identity.Contracts.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Sessions;

/// <summary>
/// Verifies that the tenant-wide session surface (GetTenantSessions) is isolated:
/// an admin in one tenant must never see another tenant's sessions. UserSessions
/// rows carry the ambient tenant filter, so cross-tenant reads should come back empty.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class SessionTenantIsolationTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public SessionTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetTenantSessions_Should_NotExposeOtherTenantsSessions()
    {
        // Arrange — create a second tenant with its own admin, and a root-tenant user
        // who logs in (creating a root-scoped session).
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"sess-iso-{uniqueId}";
        var otherAdminEmail = $"sess-iso-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);

        var rootUser = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, rootClient, "sess-iso-rootuser");
        await _auth.GetTokenAsync(rootUser.Email, rootUser.Password);

        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        // Act — the OTHER tenant's admin lists tenant sessions and searches for the root user.
        var listResponse = await otherClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/sessions?pageSize=200&includeInactive=true");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await listResponse.Content.ReadAsStringAsync();

        var searchResponse = await otherClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/sessions?search={Uri.EscapeDataString(rootUser.Email)}");
        var searchPage = await searchResponse.DeserializeAsync<PagedResponse<UserSessionDto>>();

        // Assert — the root user's session/email must not leak into the other tenant's view.
        body.ShouldNotContain(rootUser.Email);
        searchPage.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUserSessions_Should_ReturnEmpty_When_UserBelongsToAnotherTenant()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"sess-iso2-{uniqueId}";
        var otherAdminEmail = $"sess-iso2-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);

        var rootUser = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, rootClient, "sess-iso2-rootuser");
        await _auth.GetTokenAsync(rootUser.Email, rootUser.Password);

        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        // Act — the other tenant's admin asks for the root user's sessions by id.
        var response = await otherClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{rootUser.UserId}/sessions");

        // Assert — the tenant filter scopes the query to the OTHER tenant, so nothing is returned.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var sessions = await response.DeserializeAsync<List<UserSessionDto>>();
        sessions.ShouldBeEmpty();
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

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds.");
    }
}
