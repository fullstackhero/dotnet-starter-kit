using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Authentication;

[Collection(FshCollectionDefinition.Name)]
public sealed class TokenRefreshTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TokenRefreshTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task RefreshToken_Should_ReturnNewTokenPair_When_RefreshTokenIsValid()
    {
        // Arrange
        var originalToken = await _auth.GetRootAdminTokenAsync();
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/refresh");
        request.Headers.Add("tenant", TestConstants.RootTenantId);
        request.Content = JsonContent.Create(new
        {
            token = originalToken.AccessToken,
            refreshToken = originalToken.RefreshToken
        });

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newToken = await response.Content.ReadFromJsonAsync<TokenRefreshResult>();
        newToken.ShouldNotBeNull();
        newToken.Token.ShouldNotBeNullOrWhiteSpace();
        newToken.RefreshToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshToken_Should_ReturnUnauthorized_When_RefreshTokenIsInvalid()
    {
        // Arrange
        var originalToken = await _auth.GetRootAdminTokenAsync();
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/refresh");
        request.Headers.Add("tenant", TestConstants.RootTenantId);
        request.Content = JsonContent.Create(new
        {
            token = originalToken.AccessToken,
            refreshToken = "invalid-refresh-token"
        });

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
