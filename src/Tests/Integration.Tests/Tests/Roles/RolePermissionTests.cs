using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Roles;

[Collection(FshCollectionDefinition.Name)]
public sealed class RolePermissionTests
{
    private readonly AuthHelper _auth;

    public RolePermissionTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetRolePermissions_Should_ReturnOk_When_RoleExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        // Find the Admin role
        var rolesResponse = await client.GetAsync($"{TestConstants.IdentityBasePath}/roles");
        var page = await rolesResponse.DeserializeAsync<PagedResponse<RoleDto>>();
        var adminRole = page.Items.First(r => r.Name == "Admin");

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/{adminRole.Id}/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_ReturnOk_When_PermissionsAreValid()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Create a role to assign permissions to
        var createResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = string.Empty,
            name = $"PermRole-{uniqueId}",
            description = "Role for permission testing"
        });
        var createdRole = await createResponse.DeserializeAsync<RoleDto>();

        var response = await client.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/{createdRole.Id}/permissions", new
            {
                roleId = createdRole.Id,
                permissions = new[] { "Permissions.AuditTrails.View" }
            });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_Store_A_Repeated_Permission_Once()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = string.Empty,
            name = $"PermRole-{uniqueId}",
            description = "Role for duplicate permission testing"
        });
        var createdRole = await createResponse.DeserializeAsync<RoleDto>();

        // RoleClaims is unique per (RoleId, ClaimType, ClaimValue), so a repeated entry must be collapsed, not a 500.
        var response = await client.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/{createdRole.Id}/permissions", new
            {
                roleId = createdRole.Id,
                permissions = new[] { "Permissions.AuditTrails.View", "Permissions.AuditTrails.View" }
            });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"{TestConstants.IdentityBasePath}/{createdRole.Id}/permissions");
        var role = await getResponse.DeserializeAsync<RoleDto>();
        role.Permissions.ShouldNotBeNull();
        role.Permissions.Count(p => p == "Permissions.AuditTrails.View").ShouldBe(1);
    }
}
