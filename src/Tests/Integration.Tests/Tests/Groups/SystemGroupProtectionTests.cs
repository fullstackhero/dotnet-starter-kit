using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Groups;

[Collection(FshCollectionDefinition.Name)]
public sealed class SystemGroupProtectionTests
{
    private readonly AuthHelper _auth;

    public SystemGroupProtectionTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task UpdateGroup_Should_ReturnForbidden_When_GroupIsSystemGroup()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var systemGroup = await GetGroupByNameAsync(client, "All Users");

        var response = await client.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/groups/{systemGroup.Id}",
            new
            {
                name = "Renamed All Users",
                description = "Attempt to rename a seeded system group.",
                isDefault = systemGroup.IsDefault,
                roleIds = new List<string>()
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateGroup_Should_ReturnForbidden_When_GroupIsAdministrators()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var administratorsGroup = await GetGroupByNameAsync(client, "Administrators");

        var response = await client.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/groups/{administratorsGroup.Id}",
            new
            {
                name = "Renamed Administrators",
                description = "Attempt to rename the Administrators group.",
                isDefault = administratorsGroup.IsDefault,
                roleIds = new List<string>()
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteGroup_Should_ReturnForbidden_When_GroupIsSystemGroup()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var systemGroup = await GetGroupByNameAsync(client, "All Users");

        var response = await client.DeleteAsync(
            $"{TestConstants.IdentityBasePath}/groups/{systemGroup.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveUserFromGroup_Should_ReturnForbidden_When_GroupIsDefault()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var allUsersGroup = await GetGroupByNameAsync(client, "All Users");

        // Register a fresh user — UserRegistrationService auto-assigns them to every default group.
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var registerResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "DefaultGroup",
            lastName = "Member",
            email = $"defaultgroup-{uniqueId}@example.com",
            userName = $"defaultgroup-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        });
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var registered = await registerResponse.DeserializeAsync<RegisterResult>();

        var response = await client.DeleteAsync(
            $"{TestConstants.IdentityBasePath}/groups/{allUsersGroup.Id}/members/{registered.UserId}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<GroupDto> GetGroupByNameAsync(HttpClient client, string name)
    {
        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/groups");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var groups = await response.DeserializeAsync<GroupDto[]>();
        return groups.First(g => g.Name == name);
    }
}
