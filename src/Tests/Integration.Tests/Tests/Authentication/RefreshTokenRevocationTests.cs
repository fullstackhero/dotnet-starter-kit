using Integration.Tests.Infrastructure;
using FSH.Modules.Identity.Contracts.DTOs;
using System.Net.Http.Json;
using System.Net;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.Authentication;

[Collection(FshCollectionDefinition.Name)]
public sealed class RefreshTokenRevocationTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public RefreshTokenRevocationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task RevokeSession_Should_InvalidateRefreshToken_When_SessionIsRevoked()
    {
        // Arrange - Login and get tokens
        var tokenPair = await _auth.GetRootAdminTokenAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenPair.AccessToken);

        // Get current sessions
        var sessionsResponse = await client.GetAsync($"{TestConstants.IdentityBasePath}/sessions/me");
        var sessionsContent = await sessionsResponse.Content.ReadAsStringAsync();
        sessionsResponse.StatusCode.ShouldBe(HttpStatusCode.OK, $"Content: {sessionsContent}");
        var sessions = await sessionsResponse.Content.ReadFromJsonAsync<IEnumerable<UserSessionDto>>();
        // Note: isCurrentSession is currently hardcoded to false in GetUserSessionsAsync
        // so we take the first active session for this user.
        var currentSession = sessions?.FirstOrDefault();
        currentSession.ShouldNotBeNull();

        // Act - Revoke current session
        var revokeResponse = await client.DeleteAsync($"{TestConstants.IdentityBasePath}/sessions/{currentSession.Id}");
        revokeResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert - Try to refresh token
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/refresh");
        refreshRequest.Headers.Add("tenant", TestConstants.RootTenantId);
        refreshRequest.Content = JsonContent.Create(new
        {
            token = tokenPair.AccessToken,
            refreshToken = tokenPair.RefreshToken
        });

        var refreshResponse = await client.SendAsync(refreshRequest);

        // The refresh should fail because the session (and its refresh token) has been revoked
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
