using System.Text.Json;
using FSH.Modules.Identity.Contracts.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Sessions;

/// <summary>
/// Covers the admin-facing session surface: GetUserSessions, GetTenantSessions,
/// AdminRevokeSession, AdminRevokeAllSessions, and the user-facing RevokeAllSessions.
/// Sessions are created on token issuance, so each test logs a user in to populate
/// the session table before querying / revoking.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class AdminSessionManagementTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public AdminSessionManagementTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region GetUserSessions (admin)

    [Fact]
    public async Task GetUserSessions_Should_ReturnUsersSessions_When_AdminRequests()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "sess-getuser");
        // Logging in creates a session row for this user.
        await _auth.GetTokenAsync(user.Email, user.Password);

        // Act
        var response = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{user.UserId}/sessions");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var sessions = await response.DeserializeAsync<List<UserSessionDto>>();
        sessions.ShouldNotBeNull();
        sessions.Count.ShouldBeGreaterThanOrEqualTo(1);
        sessions.ShouldAllBe(s => s.UserId == user.UserId);
    }

    [Fact]
    public async Task GetUserSessions_Should_Return403_When_CallerLacksViewAllPermission()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var target = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "sess-getuser-tgt");
        var basicUser = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "sess-getuser-basic");
        using var basicClient = await _auth.CreateAuthenticatedClientAsync(basicUser.Email, basicUser.Password);

        // Act — Basic role has Sessions.View but not Sessions.ViewAll.
        var response = await basicClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{target.UserId}/sessions");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserSessions_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{Guid.NewGuid()}/sessions");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetTenantSessions (admin, paged)

    [Fact]
    public async Task GetTenantSessions_Should_ReturnPagedSessions_When_AdminRequests()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "sess-tenant");
        await _auth.GetTokenAsync(user.Email, user.Password);

        // Act
        var response = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/sessions?pageNumber=1&pageSize=50");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.DeserializeAsync<PagedResponse<UserSessionDto>>();
        page.ShouldNotBeNull();
        page.PageNumber.ShouldBe(1);
        page.PageSize.ShouldBe(50);
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetTenantSessions_Should_FilterByActiveUser_When_SearchProvided()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "sess-search");
        await _auth.GetTokenAsync(user.Email, user.Password);

        // Act — search by the user's email (ILike on user email/name/ip).
        var response = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/sessions?search={Uri.EscapeDataString(user.Email)}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.DeserializeAsync<PagedResponse<UserSessionDto>>();
        page.ShouldNotBeNull();
        page.Items.Count.ShouldBeGreaterThanOrEqualTo(1);
        page.Items.ShouldAllBe(s => s.UserEmail == user.Email);
    }

    [Fact]
    public async Task GetTenantSessions_Should_Return403_When_CallerLacksViewAllPermission()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var basicUser = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "sess-tenant-basic");
        using var basicClient = await _auth.CreateAuthenticatedClientAsync(basicUser.Email, basicUser.Password);

        // Act
        var response = await basicClient.GetAsync($"{TestConstants.IdentityBasePath}/sessions");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTenantSessions_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/sessions");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region AdminRevokeSession

    [Fact]
    public async Task AdminRevokeSession_Should_RevokeSession_When_SessionBelongsToUser()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "sess-revoke");
        await _auth.GetTokenAsync(user.Email, user.Password);

        var listResponse = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{user.UserId}/sessions");
        var sessions = await listResponse.DeserializeAsync<List<UserSessionDto>>();
        var sessionId = sessions.ShouldHaveSingleItem().Id;

        // Act
        var revoke = await adminClient.DeleteAsync(
            $"{TestConstants.IdentityBasePath}/users/{user.UserId}/sessions/{sessionId}");

        // Assert
        revoke.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var afterList = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{user.UserId}/sessions");
        var afterSessions = await afterList.DeserializeAsync<List<UserSessionDto>>();
        afterSessions.ShouldNotContain(s => s.Id == sessionId);
    }

    [Fact]
    public async Task AdminRevokeSession_Should_Return404_When_SessionDoesNotBelongToUser()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "sess-revoke-mismatch");
        await _auth.GetTokenAsync(user.Email, user.Password);

        var listResponse = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{user.UserId}/sessions");
        var sessions = await listResponse.DeserializeAsync<List<UserSessionDto>>();
        var sessionId = sessions.ShouldHaveSingleItem().Id;

        // Act — claim the real session belongs to a different (random) user id.
        var revoke = await adminClient.DeleteAsync(
            $"{TestConstants.IdentityBasePath}/users/{Guid.NewGuid()}/sessions/{sessionId}");

        // Assert
        revoke.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminRevokeSession_Should_Return403_When_CallerLacksRevokeAllPermission()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var basicUser = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "sess-revoke-basic");
        using var basicClient = await _auth.CreateAuthenticatedClientAsync(basicUser.Email, basicUser.Password);

        // Act
        var revoke = await basicClient.DeleteAsync(
            $"{TestConstants.IdentityBasePath}/users/{Guid.NewGuid()}/sessions/{Guid.NewGuid()}");

        // Assert
        revoke.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region AdminRevokeAllSessions

    [Fact]
    public async Task AdminRevokeAllSessions_Should_RevokeEverySession_When_AdminRequests()
    {
        // Arrange — issue two tokens so the user has two sessions.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "sess-revokeall");
        await _auth.GetTokenAsync(user.Email, user.Password);
        await _auth.GetTokenAsync(user.Email, user.Password);

        // Act
        var revoke = await adminClient.PostAsync(
            $"{TestConstants.IdentityBasePath}/users/{user.UserId}/sessions/revoke-all", content: null);

        // Assert
        revoke.StatusCode.ShouldBe(HttpStatusCode.OK);
        var revokedCount = await ReadRevokedCountAsync(revoke);
        revokedCount.ShouldBeGreaterThanOrEqualTo(2);

        var afterList = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{user.UserId}/sessions");
        var afterSessions = await afterList.DeserializeAsync<List<UserSessionDto>>();
        afterSessions.ShouldBeEmpty();
    }

    [Fact]
    public async Task AdminRevokeAllSessions_Should_Return403_When_CallerLacksRevokeAllPermission()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var basicUser = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "sess-revokeall-basic");
        using var basicClient = await _auth.CreateAuthenticatedClientAsync(basicUser.Email, basicUser.Password);

        // Act
        var revoke = await basicClient.PostAsync(
            $"{TestConstants.IdentityBasePath}/users/{Guid.NewGuid()}/sessions/revoke-all", content: null);

        // Assert
        revoke.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region RevokeAllSessions (self-service)

    [Fact]
    public async Task RevokeAllSessions_Should_RevokeOwnSessions_When_UserRequests()
    {
        // Arrange — give the user two sessions, then revoke all from a third client.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "sess-self-revokeall");
        await _auth.GetTokenAsync(user.Email, user.Password);
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        // Act
        var revoke = await userClient.PostAsync(
            $"{TestConstants.IdentityBasePath}/sessions/revoke-all", content: null);

        // Assert
        revoke.StatusCode.ShouldBe(HttpStatusCode.OK);
        var revokedCount = await ReadRevokedCountAsync(revoke);
        revokedCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task RevokeAllSessions_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.PostAsync(
            $"{TestConstants.IdentityBasePath}/sessions/revoke-all", content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    private static async Task<int> ReadRevokedCountAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("revokedCount").GetInt32();
    }
}
