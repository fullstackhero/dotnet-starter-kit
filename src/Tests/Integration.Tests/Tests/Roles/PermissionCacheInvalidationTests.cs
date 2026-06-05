using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;

namespace Integration.Tests.Tests.Roles;

/// <summary>
/// End-to-end proof that <c>UserPermissionService</c>'s HybridCache entry is genuinely
/// EVICTED — not merely overwritten on next miss — when a role/permission/group mutation
/// flows through the API. Each test warms the cache first (so a stale entry would survive
/// if invalidation were broken), performs the mutation through the real admin endpoint,
/// then re-reads the SAME user's effective permissions and asserts they reflect the new
/// state immediately. This is distinct from <c>RolePermissionSyncerTests</c> (claim
/// restoration into RoleClaims) — here we exercise the cache layer itself.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PermissionCacheInvalidationTests
{
    // Non-basic permission: a user with only a custom role won't have this unless the role grants it,
    // so its presence/absence is an unambiguous signal of what the cache returned.
    private const string ProbePermission = IdentityPermissions.Groups.Create;
    private const string SecondaryPermission = IdentityPermissions.Groups.Delete;

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public PermissionCacheInvalidationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Role permission mutation

    [Fact]
    public async Task UpdateRolePermissions_Should_EvictCachedPermissions_When_PermissionRemovedFromRole()
    {
        // Arrange — custom role with the probe permission, assigned to a fresh active user.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var role = await CreateRoleAsync(adminClient, $"CacheRole-{uniqueId}");
        await SetRolePermissionsAsync(adminClient, role.Id, ProbePermission);

        var (email, password, userId) = await CreateActiveUserAsync($"cacheuser-{uniqueId}");
        await AssignRoleAsync(adminClient, userId, role.Name);

        using var userClient = await _auth.CreateAuthenticatedClientAsync(email, password);

        // Warm the cache: this populates the user's HybridCache permission entry.
        var warmed = await GetOwnPermissionsAsync(userClient);
        warmed.ShouldContain(ProbePermission,
            "Pre-condition: the user must hold the probe permission via the custom role before the mutation.");

        // Act — remove the probe permission via the live admin API. UpdatePermissionsAsync
        // must call InvalidatePermissionCacheAsync for every user reachable through this role.
        await SetRolePermissionsAsync(adminClient, role.Id /* no permissions */);

        // Assert — the next read must reflect the empty state. If not evicted, the warmed entry
        // (still within its 1h expiration) would be served and still contain ProbePermission.
        var afterRevoke = await GetOwnPermissionsAsync(userClient);
        afterRevoke.ShouldNotContain(ProbePermission,
            "Cache was NOT invalidated: a removed role permission is still being served from the stale cache entry.");
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_EvictCachedPermissions_When_PermissionAddedToRole()
    {
        // Arrange — custom role that starts with NO probe permission.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var role = await CreateRoleAsync(adminClient, $"CacheAddRole-{uniqueId}");
        await SetRolePermissionsAsync(adminClient, role.Id, SecondaryPermission);

        var (email, password, userId) = await CreateActiveUserAsync($"cacheadd-{uniqueId}");
        await AssignRoleAsync(adminClient, userId, role.Name);

        using var userClient = await _auth.CreateAuthenticatedClientAsync(email, password);

        // Warm the cache WITHOUT the probe permission present.
        var warmed = await GetOwnPermissionsAsync(userClient);
        warmed.ShouldNotContain(ProbePermission,
            "Pre-condition: the user must NOT yet hold the probe permission.");

        // Act — grant the probe permission via the role.
        await SetRolePermissionsAsync(adminClient, role.Id, SecondaryPermission, ProbePermission);

        // Assert — newly granted permission appears immediately on the next read.
        var afterGrant = await GetOwnPermissionsAsync(userClient);
        afterGrant.ShouldContain(ProbePermission,
            "Cache was NOT invalidated: a newly granted role permission is not visible because the stale (pre-grant) entry was served.");
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_FlowThroughToGatedEndpoint_When_PermissionChanges()
    {
        // The strongest end-to-end signal: the authorization handler itself reads
        // through the cached HasPermissionAsync path. Prove a gated endpoint flips from
        // 403 -> 200 the instant the role grant is added — no stale deny served.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var role = await CreateRoleAsync(adminClient, $"GateRole-{uniqueId}");
        // Start with an unrelated permission so the role exists but the gate is closed.
        await SetRolePermissionsAsync(adminClient, role.Id, IdentityPermissions.Groups.View);

        var (email, password, userId) = await CreateActiveUserAsync($"gateuser-{uniqueId}");
        await AssignRoleAsync(adminClient, userId, role.Name);

        using var userClient = await _auth.CreateAuthenticatedClientAsync(email, password);

        // Warm the cache via the gated endpoint while the user LACKS Groups.Create.
        // CreateGroup is gated by IdentityPermissions.Groups.Create.
        var beforeGrant = await userClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/groups",
            new { name = $"grp-before-{uniqueId}", description = "should be forbidden" });
        beforeGrant.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "Pre-condition: the gated endpoint must reject the user before the permission is granted.");

        // Act — grant Groups.Create to the role.
        await SetRolePermissionsAsync(adminClient, role.Id, IdentityPermissions.Groups.View, IdentityPermissions.Groups.Create);

        // Assert — the gate opens on the next request. A stale cached deny would keep it at 403.
        var afterGrant = await userClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/groups",
            new { name = $"grp-after-{uniqueId}", description = "should be allowed" });
        afterGrant.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden,
            "Cache was NOT invalidated: the authorization handler is still serving a stale deny for a newly granted permission.");
        afterGrant.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    #endregion

    #region User role membership mutation

    [Fact]
    public async Task AssignUserRoles_Should_EvictCachedPermissions_When_RoleRemovedFromUser()
    {
        // Arrange — user holds a custom role granting the probe permission; warm cache.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var role = await CreateRoleAsync(adminClient, $"UnassignRole-{uniqueId}");
        await SetRolePermissionsAsync(adminClient, role.Id, ProbePermission);

        var (email, password, userId) = await CreateActiveUserAsync($"unassign-{uniqueId}");
        await AssignRoleAsync(adminClient, userId, role.Name);

        using var userClient = await _auth.CreateAuthenticatedClientAsync(email, password);

        var warmed = await GetOwnPermissionsAsync(userClient);
        warmed.ShouldContain(ProbePermission,
            "Pre-condition: the user must hold the probe permission via the assigned role.");

        // Act — remove the role from the user. UserRoleService.AssignRolesAsync must
        // invalidate the user's permission cache unconditionally.
        await UnassignRoleAsync(adminClient, userId, role.Name);

        // Assert — the probe permission is gone on the next read.
        var afterUnassign = await GetOwnPermissionsAsync(userClient);
        afterUnassign.ShouldNotContain(ProbePermission,
            "Cache was NOT invalidated on role un-assignment: the user still sees permissions from a role they no longer hold.");
    }

    #endregion

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<RoleDto> CreateRoleAsync(HttpClient adminClient, string name)
    {
        var response = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = string.Empty,
            name,
            description = "permission-cache invalidation test role"
        });
        return await response.DeserializeAsync<RoleDto>();
    }

    private static async Task SetRolePermissionsAsync(HttpClient adminClient, string roleId, params string[] permissions)
    {
        var response = await adminClient.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/{roleId}/permissions", new
            {
                roleId,
                permissions
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"Set role permissions failed: {await response.Content.ReadAsStringAsync()}");
    }

    private static async Task AssignRoleAsync(HttpClient adminClient, string userId, string roleName, bool enabled = true)
    {
        var response = await adminClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/users/{userId}/roles", new
            {
                userId,
                userRoles = new[]
                {
                    new { roleName, enabled }
                }
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"Assign role failed: {await response.Content.ReadAsStringAsync()}");
    }

    private static Task UnassignRoleAsync(HttpClient adminClient, string userId, string roleName)
        => AssignRoleAsync(adminClient, userId, roleName, enabled: false);

    private static async Task<List<string>> GetOwnPermissionsAsync(HttpClient userClient)
    {
        var response = await userClient.GetAsync($"{TestConstants.IdentityBasePath}/permissions");
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"Get current-user permissions failed: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<List<string>>();
    }

    /// <summary>
    /// Seeds a confirmed + active user (both flags are required to log in) directly via
    /// UserManager. The Finbuckle tenant context is set INLINE in this method body because
    /// it is AsyncLocal — setting it in a separate awaited helper would lose it and the
    /// tenant query filter would throw.
    /// </summary>
    private async Task<(string Email, string Password, string UserId)> CreateActiveUserAsync(string handle)
    {
        const string password = TestConstants.DefaultPassword;
        var email = $"{handle}@example.com";

        using var scope = _factory.Services.CreateScope();

        var tenant = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var user = new FshUser
        {
            FirstName = "Cache",
            LastName = "Probe",
            Email = email,
            UserName = handle,
            EmailConfirmed = true,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, password);
        result.Succeeded.ShouldBeTrue(
            $"Seeding active user failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return (email, password, user.Id);
    }
}
