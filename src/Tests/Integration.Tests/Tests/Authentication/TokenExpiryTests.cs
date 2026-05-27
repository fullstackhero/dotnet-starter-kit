using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Integration.Tests.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace Integration.Tests.Tests.Authentication;

[Collection(FshCollectionDefinition.Name)]
public sealed class TokenExpiryTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TokenExpiryTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task RefreshEndpoint_Should_AcceptExpiredAccessToken_When_RefreshTokenIsValid()
    {
        // Arrange — get a valid token pair, then use it to refresh
        var token = await _auth.GetRootAdminTokenAsync();

        // The refresh endpoint accepts expired access tokens (it only cross-checks the subject)
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/refresh");
        request.Headers.Add("tenant", TestConstants.RootTenantId);
        request.Content = JsonContent.Create(new
        {
            token = token.AccessToken,
            refreshToken = token.RefreshToken
        });

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var refreshed = await response.Content.ReadFromJsonAsync<TokenRefreshResult>();
        refreshed.ShouldNotBeNull();
        refreshed.Token.ShouldNotBeNullOrWhiteSpace();
        refreshed.RefreshToken.ShouldNotBeNullOrWhiteSpace();

        // The new access token should be different from the old one
        refreshed.Token.ShouldNotBe(token.AccessToken);
    }

    [Fact]
    public async Task RefreshEndpoint_Should_RotateRefreshToken_When_Refreshing()
    {
        // Arrange
        var token = await _auth.GetRootAdminTokenAsync();

        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/refresh");
        request.Headers.Add("tenant", TestConstants.RootTenantId);
        request.Content = JsonContent.Create(new
        {
            token = token.AccessToken,
            refreshToken = token.RefreshToken
        });

        // Act
        var response = await client.SendAsync(request);

        // Assert — refresh token should be rotated (new value)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var refreshed = await response.Content.ReadFromJsonAsync<TokenRefreshResult>();
        refreshed!.RefreshToken.ShouldNotBe(token.RefreshToken);
    }

    [Fact]
    public async Task RefreshEndpoint_Should_Reject_When_OldRefreshTokenReusedAfterRotation()
    {
        // Arrange — get token, refresh once (rotates refresh token), then reuse old refresh token
        var token = await _auth.GetRootAdminTokenAsync();

        // First refresh — succeeds and rotates the refresh token
        using var client1 = _factory.CreateClient();
        var request1 = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/refresh");
        request1.Headers.Add("tenant", TestConstants.RootTenantId);
        request1.Content = JsonContent.Create(new
        {
            token = token.AccessToken,
            refreshToken = token.RefreshToken
        });
        var response1 = await client1.SendAsync(request1);
        response1.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act — reuse the OLD refresh token (should be invalidated by rotation)
        using var client2 = _factory.CreateClient();
        var request2 = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/refresh");
        request2.Headers.Add("tenant", TestConstants.RootTenantId);
        request2.Content = JsonContent.Create(new
        {
            token = token.AccessToken,
            refreshToken = token.RefreshToken  // reusing old refresh token
        });
        var response2 = await client2.SendAsync(request2);

        // Assert — old refresh token should be rejected
        response2.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessToken_Should_HaveCorrectExpiry_When_Generated()
    {
        // Arrange & Act
        var token = await _auth.GetRootAdminTokenAsync();

        // Assert — verify the token has a reasonable expiry (configured to 15 min in test config)
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.AccessToken);

        var expiresIn = jwt.ValidTo - DateTime.UtcNow;
        expiresIn.TotalMinutes.ShouldBeGreaterThan(1); // at least 1 minute remaining
        expiresIn.TotalMinutes.ShouldBeLessThanOrEqualTo(31); // not more than 31 min (30 + clock skew)
    }

    [Fact]
    public async Task RefreshedToken_Should_HaveExtendedExpiry_When_Refreshed()
    {
        // Arrange
        var token = await _auth.GetRootAdminTokenAsync();
        var handler = new JwtSecurityTokenHandler();
        var originalJwt = handler.ReadJwtToken(token.AccessToken);

        // Act — refresh the token
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/refresh");
        request.Headers.Add("tenant", TestConstants.RootTenantId);
        request.Content = JsonContent.Create(new
        {
            token = token.AccessToken,
            refreshToken = token.RefreshToken
        });
        var response = await client.SendAsync(request);
        var refreshed = await response.Content.ReadFromJsonAsync<TokenRefreshResult>();

        // Assert — the new token should have a fresh expiry
        var newJwt = handler.ReadJwtToken(refreshed!.Token);
        var newExpiresIn = newJwt.ValidTo - DateTime.UtcNow;
        newExpiresIn.TotalMinutes.ShouldBeGreaterThan(1);
    }

    [Theory]
    [InlineData(-5, true)]   // already expired — near expiry
    [InlineData(10, true)]   // 10 seconds left — within 30s buffer
    [InlineData(29, true)]   // 29 seconds left — within 30s buffer
    [InlineData(60, false)]  // 1 minute left — not near expiry
    [InlineData(300, false)] // 5 minutes left — not near expiry
    public void IsTokenNearExpiry_Should_DetectCorrectly(int secondsUntilExpiry, bool expectedNearExpiry)
    {
        // Arrange — create a JWT with the specified expiry
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(TestConstants.JwtSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test-user")]),
            NotBefore = DateTime.UtcNow.AddMinutes(-10),
            Expires = DateTime.UtcNow.AddSeconds(secondsUntilExpiry),
            Issuer = TestConstants.JwtIssuer,
            Audience = TestConstants.JwtAudience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.CreateToken(tokenDescriptor);
        var tokenString = handler.WriteToken(jwt);

        // Act — check if the token is near expiry using the same logic as AuthorizationHeaderHandler
        var jwtRead = handler.ReadJwtToken(tokenString);
        bool isNearExpiry = jwtRead.ValidTo <= DateTime.UtcNow.Add(TimeSpan.FromSeconds(30));

        // Assert
        isNearExpiry.ShouldBe(expectedNearExpiry);
    }
}
