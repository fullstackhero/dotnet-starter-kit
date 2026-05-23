using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Roles;

/// <summary>
/// Roles are tenant-scoped: a custom role created in one tenant must be completely
/// invisible to another tenant. Verifies both the list endpoint (the role does not
/// appear in tenant B's roles) and direct lookup (tenant B cannot read the role by id).
/// Cross-tenant scaffolding mirrors <see cref="Webhooks.WebhookTenantIsolationTests"/>.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class RoleTenantIsolationTests
{
    private readonly AuthHelper _auth;

    public RoleTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetRoles_Should_NotReturnRoleCreatedInAnotherTenant()
    {
        // Arrange — a fresh tenant B with its own admin, and a custom role created in
        // the root tenant (tenant A).
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"role-iso-{uniqueId}";
        var otherAdminEmail = $"role-iso-admin-{uniqueId}@tenant.com";

        var rootRoleName = $"RootOnlyRole-{uniqueId}";
        var rootRole = await CreateRoleAsync(rootClient, rootRoleName);

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);
        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        // Act — list roles as tenant B's admin.
        var otherRolesResponse = await otherClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/roles?pageNumber=1&pageSize=200");

        // Assert — tenant B never sees the role created in tenant A.
        otherRolesResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var otherRoles = await otherRolesResponse.DeserializeAsync<PagedResponse<RoleDto>>();
        otherRoles.Items.ShouldNotContain(r => r.Name == rootRoleName,
            "A role created in the root tenant leaked into another tenant's role list.");
        otherRoles.Items.ShouldNotContain(r => r.Id == rootRole.Id,
            "A role id created in the root tenant leaked into another tenant's role list.");

        // Sanity: the role IS visible to the tenant that created it.
        var rootRolesResponse = await rootClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/roles?pageNumber=1&pageSize=200");
        var rootRoles = await rootRolesResponse.DeserializeAsync<PagedResponse<RoleDto>>();
        rootRoles.Items.ShouldContain(r => r.Name == rootRoleName,
            "The role must remain visible to the tenant that owns it.");
    }

    [Fact]
    public async Task GetRoleById_Should_Return404_When_RoleBelongsToAnotherTenant()
    {
        // Arrange — role created in root tenant, fresh tenant B provisioned.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var otherTenantId = $"role-byid-{uniqueId}";
        var otherAdminEmail = $"role-byid-admin-{uniqueId}@tenant.com";

        var rootRole = await CreateRoleAsync(rootClient, $"RootByIdRole-{uniqueId}");

        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);
        using var otherClient = await CreateTenantAdminClientWithRetryAsync(
            otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);

        // Act — tenant B attempts to read tenant A's role by id.
        var crossLookup = await otherClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/roles/{rootRole.Id}");

        // Assert — the role is not found in tenant B's scope.
        crossLookup.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Tenant B was able to read a role that belongs to the root tenant.");

        // Sanity: the owning tenant can read it.
        var ownLookup = await rootClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/roles/{rootRole.Id}");
        ownLookup.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<RoleDto> CreateRoleAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = string.Empty,
            name,
            description = "tenant-isolation test role"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"Create role failed: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<RoleDto>();
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
