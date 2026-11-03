using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;

namespace Integration.Tests.Tests.Authorization;

/// <summary>
/// <c>.RequirePermission(a, b)</c> must demand every listed permission, and a route group's
/// <c>.RequirePermission(a)</c> must still apply when an endpoint inside it adds <c>.RequirePermission(b)</c>.
/// Probed through test-only endpoints from <see cref="TestPermissionEndpointStartupFilter"/>, because no
/// production endpoint lists more than one permission or gates a group.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class RequirePermissionAllOfTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public RequirePermissionAllOfTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task TwoPermissionEndpoint_Should_Return403_When_UserHoldsOnlyTheFirstPermission()
    {
        using var userClient = await CreateUserClientWithPermissionsAsync(
            "allof-first", TestPermissionEndpointStartupFilter.FirstPermission);

        var response = await userClient.GetAsync(TestPermissionEndpointStartupFilter.TwoPermissionsPath);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "Holding only the first of two required permissions must not open the gate.");
    }

    [Fact]
    public async Task TwoPermissionEndpoint_Should_Return200_When_UserHoldsBothPermissions()
    {
        using var userClient = await CreateUserClientWithPermissionsAsync(
            "allof-both",
            TestPermissionEndpointStartupFilter.FirstPermission,
            TestPermissionEndpointStartupFilter.SecondPermission);

        var response = await userClient.GetAsync(TestPermissionEndpointStartupFilter.TwoPermissionsPath);

        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"Holding every required permission must open the gate: {await response.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task GroupGatedEndpoint_Should_Return403_When_UserHoldsOnlyTheEndpointPermission()
    {
        using var userClient = await CreateUserClientWithPermissionsAsync(
            "group-endpoint-only", TestPermissionEndpointStartupFilter.SecondPermission);

        var response = await userClient.GetAsync(TestPermissionEndpointStartupFilter.GroupGatedPath);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "The endpoint's RequirePermission must not replace the route group's gate.");
    }

    [Fact]
    public async Task GroupGatedEndpoint_Should_Return403_When_UserHoldsOnlyTheGroupPermission()
    {
        using var userClient = await CreateUserClientWithPermissionsAsync(
            "group-group-only", TestPermissionEndpointStartupFilter.FirstPermission);

        var response = await userClient.GetAsync(TestPermissionEndpointStartupFilter.GroupGatedPath);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "Holding only the group's permission must not open an endpoint that adds its own gate.");
    }

    [Fact]
    public async Task GroupGatedEndpoint_Should_Return200_When_UserHoldsGroupAndEndpointPermissions()
    {
        using var userClient = await CreateUserClientWithPermissionsAsync(
            "group-both",
            TestPermissionEndpointStartupFilter.FirstPermission,
            TestPermissionEndpointStartupFilter.SecondPermission);

        var response = await userClient.GetAsync(TestPermissionEndpointStartupFilter.GroupGatedPath);

        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"Holding both the group and endpoint permissions must open the gate: {await response.Content.ReadAsStringAsync()}");
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private async Task<HttpClient> CreateUserClientWithPermissionsAsync(string handlePrefix, params string[] permissions)
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var role = await CreateRoleAsync(adminClient, $"AllOfRole-{uniqueId}");
        await SetRolePermissionsAsync(adminClient, role.Id, permissions);

        var (email, password, userId) = await CreateActiveUserAsync($"{handlePrefix}-{uniqueId}");
        await AssignRoleAsync(adminClient, userId, role.Name);

        return await _auth.CreateAuthenticatedClientAsync(email, password);
    }

    private static async Task<RoleDto> CreateRoleAsync(HttpClient adminClient, string name)
    {
        var response = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = string.Empty,
            name,
            description = "require-permission all-of test role"
        });
        return await response.DeserializeAsync<RoleDto>();
    }

    private static async Task SetRolePermissionsAsync(HttpClient adminClient, string roleId, string[] permissions)
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

    private static async Task AssignRoleAsync(HttpClient adminClient, string userId, string roleName)
    {
        var response = await adminClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/users/{userId}/roles", new
            {
                userId,
                userRoles = new[]
                {
                    new { roleName, enabled = true }
                }
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"Assign role failed: {await response.Content.ReadAsStringAsync()}");
    }

    /// <summary>
    /// Seeds a confirmed + active user directly via UserManager. The Finbuckle tenant context is
    /// set INLINE because it is AsyncLocal; an awaited helper would lose it.
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
            FirstName = "AllOf",
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
