using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Integration.Tests.Tests.Sessions;
using UserRoleDto = FSH.Modules.Identity.Contracts.DTOs.UserRoleDto;

namespace Integration.Tests.Tests.Users;

/// <summary>
/// Covers the user read surface: GetUserRoles, GetUserGroups, and SearchUsers
/// (filtering, sorting, paging, authz).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class UserQueryTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public UserQueryTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region GetUserRoles

    [Fact]
    public async Task GetUserRoles_Should_ReturnRolesWithEnabledFlags_When_UserExists()
    {
        // Arrange — a freshly registered user has the Basic role enabled.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "roles-get");

        // Act
        var response = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{user.UserId}/roles");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var roles = await response.DeserializeAsync<List<UserRoleDto>>();
        roles.ShouldNotBeNull();
        roles.ShouldContain(r => r.RoleName == "Basic" && r.Enabled);
        roles.ShouldContain(r => r.RoleName == "Admin" && !r.Enabled);
    }

    [Fact]
    public async Task GetUserRoles_Should_Return404_When_UserDoesNotExist()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{Guid.NewGuid()}/roles");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserRoles_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{Guid.NewGuid()}/roles");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetUserGroups

    [Fact]
    public async Task GetUserGroups_Should_ReturnGroups_When_UserExists()
    {
        // Arrange — new users are auto-added to default groups on registration.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "groups-get");

        // Act
        var response = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{user.UserId}/groups");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var groups = await response.DeserializeAsync<List<GroupDto>>();
        groups.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetUserGroups_Should_Return404_When_UserDoesNotExist()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{Guid.NewGuid()}/groups");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserGroups_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{Guid.NewGuid()}/groups");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region SearchUsers

    [Fact]
    public async Task SearchUsers_Should_FindUserByEmailFragment_When_SearchProvided()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "search-byemail");

        // Act
        var response = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/search?search={Uri.EscapeDataString(user.Email)}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.DeserializeAsync<PagedResponse<UserDto>>();
        page.Items.ShouldContain(u => u.Email == user.Email);
    }

    [Fact]
    public async Task SearchUsers_Should_FilterByActiveStatus_When_IsActiveProvided()
    {
        // Arrange — deactivate a user, then search active-only.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "search-inactive");

        var deactivate = new HttpRequestMessage(HttpMethod.Patch,
            $"{TestConstants.IdentityBasePath}/users/{user.UserId}")
        {
            Content = JsonContent.Create(new { isActive = false })
        };
        (await adminClient.SendAsync(deactivate)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act — scope each query to this user's email so the assertions are deterministic
        // regardless of how many other users exist in the tenant.
        var search = Uri.EscapeDataString(user.Email);
        var activeResponse = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/search?isActive=true&search={search}&pageSize=100");
        var inactiveResponse = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/search?isActive=false&search={search}&pageSize=100");

        // Assert — the now-inactive user appears only in the inactive result set.
        var activePage = await activeResponse.DeserializeAsync<PagedResponse<UserDto>>();
        var inactivePage = await inactiveResponse.DeserializeAsync<PagedResponse<UserDto>>();
        activePage.Items.ShouldNotContain(u => u.Id == user.UserId);
        inactivePage.Items.ShouldContain(u => u.Id == user.UserId);
    }

    [Fact]
    public async Task SearchUsers_Should_RespectPaging_When_PageSizeProvided()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/search?pageNumber=1&pageSize=1");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.DeserializeAsync<PagedResponse<UserDto>>();
        page.PageNumber.ShouldBe(1);
        page.PageSize.ShouldBe(1);
        page.Items.Count.ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task SearchUsers_Should_SortByLastNameDescending_When_SortPrefixedWithDash()
    {
        // Arrange — seed two users sharing a unique surname prefix so we can assert ordering
        // deterministically via the search filter.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var prefix = $"zsort{Guid.NewGuid():N}"[..16];
        await SeedUserWithNamesAsync(adminClient, "Alpha", $"{prefix}aaa");
        await SeedUserWithNamesAsync(adminClient, "Beta", $"{prefix}zzz");

        // Act — last name descending: "zzz" must precede "aaa".
        var response = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/search?sort=-lastname&search={prefix}&pageSize=100");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.DeserializeAsync<PagedResponse<UserDto>>();
        var lastNames = page.Items.Select(u => u.LastName).ToList();
        lastNames.ShouldBe(lastNames.OrderByDescending(n => n, StringComparer.Ordinal).ToList());
        lastNames[0].ShouldBe($"{prefix}zzz");
    }

    [Fact]
    public async Task SearchUsers_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/users/search");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task SeedUserWithNamesAsync(HttpClient adminClient, string firstName, string lastName)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        const string password = "Test@1234!";
        var response = await adminClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/register", new
            {
                firstName,
                lastName,
                email = $"{lastName}-{uniqueId}@example.com",
                userName = $"{lastName}-{uniqueId}",
                password,
                confirmPassword = password
            });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    #endregion
}
