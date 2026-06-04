using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Users;

[Collection(FshCollectionDefinition.Name)]
public sealed class UserManagementGuardTests
{
    private readonly AuthHelper _auth;

    public UserManagementGuardTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task DeleteUser_Should_ReturnBadRequest_When_TargetIsSelf()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var profile = await GetCurrentProfileAsync(client);

        var response = await client.DeleteAsync($"{TestConstants.IdentityBasePath}/users/{profile.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteUser_Should_ReturnBadRequest_When_TargetIsAdmin()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Register a fresh user
        var registerResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "Other",
            lastName = "Admin",
            email = $"otheradmin-{uniqueId}@example.com",
            userName = $"otheradmin-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        });
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var registered = await registerResponse.DeserializeAsync<RegisterResult>();

        // Promote the new user to Admin
        var (adminRole, basicRole) = await GetSystemRolesAsync(client);
        var assignResponse = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/users/{registered.UserId}/roles",
            new
            {
                userId = registered.UserId,
                userRoles = new[]
                {
                    new { roleId = adminRole.Id, roleName = "Admin", enabled = true },
                    new { roleId = basicRole.Id, roleName = "Basic", enabled = true }
                }
            });
        assignResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Root admin tries to delete the second admin → must be rejected.
        var deleteResponse = await client.DeleteAsync($"{TestConstants.IdentityBasePath}/users/{registered.UserId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignUserRoles_Should_ReturnBadRequest_When_RemovingOwnAdminRole()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var profile = await GetCurrentProfileAsync(client);
        var (adminRole, _) = await GetSystemRolesAsync(client);

        var response = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/users/{profile.Id}/roles",
            new
            {
                userId = profile.Id,
                userRoles = new[]
                {
                    new { roleId = adminRole.Id, roleName = "Admin", enabled = false }
                }
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static async Task<UserDto> GetCurrentProfileAsync(HttpClient client)
    {
        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.DeserializeAsync<UserDto>();
    }

    private static async Task<(RoleDto Admin, RoleDto Basic)> GetSystemRolesAsync(HttpClient client)
    {
        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/roles");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.DeserializeAsync<PagedResponse<RoleDto>>();
        return (page.Items.First(r => r.Name == "Admin"), page.Items.First(r => r.Name == "Basic"));
    }
}
