using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Groups;

[Collection(FshCollectionDefinition.Name)]
public sealed class GroupCrudTests
{
    private readonly AuthHelper _auth;

    public GroupCrudTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetGroups_Should_ReturnOk_When_Authenticated()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/groups?pageNumber=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateGroup_Should_Return201WithGroup_When_DataIsValid()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/groups", new
        {
            name = $"TestGroup-{uniqueId}",
            description = "Integration test group",
            isDefault = false,
            roleIds = new List<string>()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var group = await response.DeserializeAsync<GroupDto>();
        group.Name.ShouldStartWith("TestGroup-");
    }

    [Fact]
    public async Task UpdateGroup_Should_ReturnOk_When_GroupExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/groups", new
        {
            name = $"UpdateMe-{uniqueId}",
            description = "To be updated",
            isDefault = false,
            roleIds = new List<string>()
        });
        var createdGroup = await createResponse.DeserializeAsync<GroupDto>();

        var response = await client.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/groups/{createdGroup.Id}", new
            {
                name = $"Updated-{uniqueId}",
                description = "Updated description",
                isDefault = false,
                roleIds = new List<string>()
            });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteGroup_Should_ReturnNoContent_When_GroupIsNotSystemGroup()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/groups", new
        {
            name = $"DeleteMe-{uniqueId}",
            description = "To be deleted",
            isDefault = false,
            roleIds = new List<string>()
        });
        var createdGroup = await createResponse.DeserializeAsync<GroupDto>();

        var response = await client.DeleteAsync($"{TestConstants.IdentityBasePath}/groups/{createdGroup.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
