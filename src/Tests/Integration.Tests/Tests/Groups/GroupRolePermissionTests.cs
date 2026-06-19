using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;

namespace Integration.Tests.Tests.Groups;

/// <summary>
/// Proves group-derived roles confer PERMISSIONS, not just JWT role claims.
/// <c>IdentityService.AddRoleClaimsAsync</c> unions direct roles with
/// <c>IGroupRoleService.GetUserGroupRolesAsync</c> when minting tokens, and every group
/// mutation handler invalidates the permission cache — so <c>UserPermissionService</c>
/// must resolve the same union, or a user whose only role comes via a group fails every
/// <c>.RequirePermission()</c> gate despite their JWT showing the role.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class GroupRolePermissionTests
{
    private const string ProbePermission = IdentityPermissions.Groups.Create;

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public GroupRolePermissionTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetOwnPermissions_Should_IncludeGroupRolePermissions_When_UserHasNoDirectRoles()
    {
        // Arrange — custom role with the probe permission, attached to a GROUP (never to the user).
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var role = await CreateRoleWithPermissionAsync(adminClient, $"GrpPermRole-{uniqueId}", ProbePermission);
        var groupId = await CreateGroupWithRoleAsync(adminClient, $"PermGroup-{uniqueId}", role.Id);

        var (email, password, userId) = await CreateActiveUserAsync($"grpperm-{uniqueId}");
        await AddUserToGroupAsync(adminClient, groupId, userId);

        using var userClient = await _auth.CreateAuthenticatedClientAsync(email, password);

        // Act — read the user's own effective permissions (auth-only endpoint, backed by UserPermissionService).
        var response = await userClient.GetAsync($"{TestConstants.IdentityBasePath}/permissions");
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"Get current-user permissions failed: {await response.Content.ReadAsStringAsync()}");
        var permissions = await response.DeserializeAsync<List<string>>();

        // Assert — the group-derived role's permission must be in the effective set.
        permissions.ShouldContain(ProbePermission,
            "UserPermissionService only resolves DIRECT role assignments: a role granted via group membership " +
            "confers no permissions, even though the JWT carries the role claim.");
    }

    [Fact]
    public async Task PermissionGate_Should_Open_When_UsersOnlyRoleComesViaGroup()
    {
        // Arrange — same setup, but probe through a real .RequirePermission() gate
        // (POST /groups requires Groups.Create), exercising the authorization handler path.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var role = await CreateRoleWithPermissionAsync(adminClient, $"GrpGateRole-{uniqueId}", ProbePermission);
        var groupId = await CreateGroupWithRoleAsync(adminClient, $"GateGroup-{uniqueId}", role.Id);

        var (email, password, userId) = await CreateActiveUserAsync($"grpgate-{uniqueId}");
        await AddUserToGroupAsync(adminClient, groupId, userId);

        using var userClient = await _auth.CreateAuthenticatedClientAsync(email, password);

        // Act — hit the gated endpoint with the group-conferred permission.
        var response = await userClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/groups", new
        {
            name = $"grp-by-member-{uniqueId}",
            description = "created via group-derived permission",
            isDefault = false,
            roleIds = new List<string>()
        });

        // Assert — the gate must honor the group-derived role.
        response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden,
            "RequiredPermissionAuthorizationHandler denied a user whose group membership grants a role holding the required permission.");
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<RoleDto> CreateRoleWithPermissionAsync(HttpClient adminClient, string name, string permission)
    {
        var createResponse = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = string.Empty,
            name,
            description = "group-role permission test role"
        });
        var role = await createResponse.DeserializeAsync<RoleDto>();

        var permResponse = await adminClient.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/{role.Id}/permissions", new
            {
                roleId = role.Id,
                permissions = new[] { permission }
            });
        permResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"Set role permissions failed: {await permResponse.Content.ReadAsStringAsync()}");

        return role;
    }

    private static async Task<Guid> CreateGroupWithRoleAsync(HttpClient adminClient, string name, string roleId)
    {
        var response = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/groups", new
        {
            name,
            description = "group-role permission test group",
            isDefault = false,
            roleIds = new List<string> { roleId }
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created,
            $"Create group failed: {await response.Content.ReadAsStringAsync()}");
        var group = await response.DeserializeAsync<GroupDto>();
        return group.Id;
    }

    private static async Task AddUserToGroupAsync(HttpClient adminClient, Guid groupId, string userId)
    {
        var response = await adminClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/groups/{groupId}/members",
            new { userIds = new[] { userId } });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"Add user to group failed: {await response.Content.ReadAsStringAsync()}");
    }

    /// <summary>
    /// Seeds a confirmed + active user with NO role assignments. The Finbuckle tenant
    /// context is set INLINE because it is AsyncLocal — setting it in an awaited helper
    /// would lose it and the tenant query filter would throw.
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
            FirstName = "GroupRole",
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
