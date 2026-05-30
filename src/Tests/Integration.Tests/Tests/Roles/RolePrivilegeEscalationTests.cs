using FSH.Modules.Identity.Contracts.Authorization;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Roles;

/// <summary>
/// Privilege-escalation guard. A NON-root tenant admin legitimately holds Roles.Create/Update, but
/// must NOT be able to grant ROOT-only permissions (Permissions.Tenants.* / Permissions.Platform.*)
/// to a role. <c>RoleService.FilterRootPermissions</c> previously stripped only names with a
/// "Permissions.Root." prefix — which matches NO real root permission — so the root permission was
/// persisted and the caller could escalate to operating on every tenant. These tests encode that
/// exact attack and the root-operator's legitimate counterpart.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class RolePrivilegeEscalationTests
{
    // A real IsRoot permission (MultitenancyPermissions.Tenants.Update — flagged IsRoot: true).
    private const string RootPermission = "Permissions.Tenants.Update";
    private const string AllowedPermission = IdentityPermissions.Groups.View;

    private readonly AuthHelper _auth;

    public RolePrivilegeEscalationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_StripRootPermissions_When_CallerIsNonRootTenantAdmin()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"esc-{unique}";
        var adminEmail = $"esc-admin-{unique}@tenant.com";
        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await WaitForProvisioningAsync(rootClient, tenantId);

        using var tenantAdmin = await CreateTenantAdminClientWithRetryAsync(
            adminEmail, TestConstants.DefaultPassword, tenantId);

        // The tenant admin creates a role and attempts to grant it a ROOT-only permission alongside a
        // legitimate non-root one.
        var roleId = await CreateRoleAsync(tenantAdmin, $"EscRole-{unique}");
        await SetRolePermissionsAsync(tenantAdmin, roleId, RootPermission, AllowedPermission);

        var persisted = await GetRolePermissionsBodyAsync(tenantAdmin, roleId);
        persisted.Contains(RootPermission, StringComparison.Ordinal).ShouldBeFalse(
            "a non-root tenant admin must not be able to grant a root-only permission to a role");
        persisted.Contains(AllowedPermission, StringComparison.Ordinal).ShouldBeTrue(
            "non-root permissions in the same request must still be applied");
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_AllowRootPermissions_When_CallerIsRootOperator()
    {
        // Sanity counterpart: the root operator CAN assign root-only permissions; the filter only
        // restricts non-root callers.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var roleId = await CreateRoleAsync(rootClient, $"RootEscRole-{unique}");
        await SetRolePermissionsAsync(rootClient, roleId, RootPermission);

        var persisted = await GetRolePermissionsBodyAsync(rootClient, roleId);
        persisted.Contains(RootPermission, StringComparison.Ordinal).ShouldBeTrue(
            "the root operator may assign root-only permissions");
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<string> CreateRoleAsync(HttpClient client, string name)
    {
        var resp = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles",
            new { id = string.Empty, name, description = "escalation test role" });
        resp.IsSuccessStatusCode.ShouldBeTrue($"Create role failed: {await resp.Content.ReadAsStringAsync()}");
        var role = await resp.DeserializeAsync<RoleRow>();
        return role.Id;
    }

    private static async Task SetRolePermissionsAsync(HttpClient client, string roleId, params string[] permissions)
    {
        var resp = await client.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/{roleId}/permissions", new { roleId, permissions });
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
    }

    private static async Task<string> GetRolePermissionsBodyAsync(HttpClient client, string roleId)
    {
        var resp = await client.GetAsync($"{TestConstants.IdentityBasePath}/{roleId}/permissions");
        resp.IsSuccessStatusCode.ShouldBeTrue($"Get role permissions failed: {await resp.Content.ReadAsStringAsync()}");
        return await resp.Content.ReadAsStringAsync();
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

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var resp = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        resp.StatusCode.ShouldBe(HttpStatusCode.Created, await resp.Content.ReadAsStringAsync());
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            var resp = await client.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync();
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

    private sealed record RoleRow
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }
}
