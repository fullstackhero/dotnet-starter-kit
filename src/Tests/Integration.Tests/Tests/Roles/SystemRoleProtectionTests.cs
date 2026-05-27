using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Roles;

[Collection(FshCollectionDefinition.Name)]
public sealed class SystemRoleProtectionTests
{
    private readonly AuthHelper _auth;

    public SystemRoleProtectionTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task DeleteRole_Should_ReturnBadRequest_When_RoleIsAdmin()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var adminRole = await GetRoleAsync(client, "Admin");

        var response = await client.DeleteAsync($"{TestConstants.IdentityBasePath}/roles/{adminRole.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteRole_Should_ReturnBadRequest_When_RoleIsBasic()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var basicRole = await GetRoleAsync(client, "Basic");

        var response = await client.DeleteAsync($"{TestConstants.IdentityBasePath}/roles/{basicRole.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpsertRole_Should_ReturnBadRequest_When_RenamingSystemRole()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var adminRole = await GetRoleAsync(client, "Admin");

        var response = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = adminRole.Id,
            name = "RenamedAdmin",
            description = "Attempt to rename the Admin role"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpsertRole_Should_ReturnBadRequest_When_CreatingWithSystemName()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = string.Empty,
            name = "Admin",
            description = "Attempt to create a role using the system name"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpsertRole_Should_ReturnBadRequest_When_RenamingCustomRoleToSystemName()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = string.Empty,
            name = $"Custom-{uniqueId}",
            description = "A custom role"
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var custom = await createResponse.DeserializeAsync<RoleDto>();

        var renameResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = custom.Id,
            name = "Basic",
            description = "Attempt to hijack the system role name"
        });

        renameResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_ReturnBadRequest_When_RoleIsBasic()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var basicRole = await GetRoleAsync(client, "Basic");

        var response = await client.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/{basicRole.Id}/permissions",
            new
            {
                roleId = basicRole.Id,
                permissions = new[] { "Permissions.Users.Create" }
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_ReturnBadRequest_When_RoleIsAdmin()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var adminRole = await GetRoleAsync(client, "Admin");

        var response = await client.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/{adminRole.Id}/permissions",
            new
            {
                roleId = adminRole.Id,
                permissions = new[] { "Permissions.Users.Create" }
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static async Task<RoleDto> GetRoleAsync(HttpClient client, string name)
    {
        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/roles");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.DeserializeAsync<PagedResponse<RoleDto>>();
        return page.Items.First(r => r.Name == name);
    }
}
