using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Roles;

[Collection(FshCollectionDefinition.Name)]
public sealed class RoleManagementTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public RoleManagementTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetRoles_Should_ReturnSeededAdminAndBasicRoles_When_Authenticated()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/roles");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.DeserializeAsync<PagedResponse<RoleDto>>();
        page.Items.Count.ShouldBeGreaterThanOrEqualTo(2);
        page.Items.ShouldContain(r => r.Name == "Admin");
        page.Items.ShouldContain(r => r.Name == "Basic");
    }

    [Fact]
    public async Task CreateRole_Should_ReturnCreatedRole_When_NameIsUnique()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = string.Empty,
            name = $"TestRole-{uniqueId}",
            description = "Integration test role"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var role = await response.DeserializeAsync<RoleDto>();
        role.Name.ShouldStartWith("TestRole-");
    }

    [Fact]
    public async Task DeleteRole_Should_ReturnNoContent_When_RoleExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = string.Empty,
            name = $"DeleteMe-{uniqueId}",
            description = "Role to delete"
        });
        var createdRole = await createResponse.DeserializeAsync<RoleDto>();

        var response = await client.DeleteAsync($"{TestConstants.IdentityBasePath}/roles/{createdRole.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetRoles_Should_Return401_When_NotAuthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/roles");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
