using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Users;

[Collection(FshCollectionDefinition.Name)]
public sealed class UserManagementTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public UserManagementTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetProfile_Should_ReturnCurrentUser_When_Authenticated()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/profile");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await response.DeserializeAsync<UserDto>();
        user.Email.ShouldBe(TestConstants.RootAdminEmail, StringCompareShould.IgnoreCase);
    }

    [Fact]
    public async Task GetProfile_Should_Return401_When_NotAuthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/profile");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_Should_ReturnPagedList_When_Authenticated()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/users?pageNumber=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserById_Should_ReturnMatchingUser_When_UserExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Create a user
        var registerResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "GetById",
            lastName = "Test",
            email = $"getbyid-{uniqueId}@example.com",
            userName = $"getbyid-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        });
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var registered = await registerResponse.DeserializeAsync<RegisterResult>();

        // Retrieve the user by ID
        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/users/{registered.UserId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await response.DeserializeAsync<UserDto>();
        user.FirstName.ShouldBe("GetById");
        user.LastName.ShouldBe("Test");
    }

    [Fact]
    public async Task ToggleUserStatus_Should_ReturnNoContent_When_DeactivatingUser()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var registerResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "Toggle",
            lastName = "Status",
            email = $"toggle-{uniqueId}@example.com",
            userName = $"toggle-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        });
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var registered = await registerResponse.DeserializeAsync<RegisterResult>();

        // PATCH /users/{id} toggles status
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{TestConstants.IdentityBasePath}/users/{registered.UserId}")
        {
            Content = JsonContent.Create(new { isActive = false })
        };

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteUser_Should_ReturnNoContent_When_UserExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var registerResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "Delete",
            lastName = "Test",
            email = $"delete-{uniqueId}@example.com",
            userName = $"delete-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        });
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var registered = await registerResponse.DeserializeAsync<RegisterResult>();

        var response = await client.DeleteAsync($"{TestConstants.IdentityBasePath}/users/{registered.UserId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
