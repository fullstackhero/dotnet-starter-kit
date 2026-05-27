using System.IdentityModel.Tokens.Jwt;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Authentication;

[Collection(FshCollectionDefinition.Name)]
public sealed class TokenGenerationTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TokenGenerationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GenerateToken_Should_ReturnAccessAndRefreshToken_When_CredentialsAreValid()
    {
        var token = await _auth.GetRootAdminTokenAsync();

        token.ShouldNotBeNull();
        token.AccessToken.ShouldNotBeNullOrWhiteSpace();
        token.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        token.AccessTokenExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow);
        token.RefreshTokenExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateToken_Should_Return401_When_PasswordIsIncorrect()
    {
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
        request.Headers.Add("tenant", TestConstants.RootTenantId);
        request.Content = JsonContent.Create(new
        {
            email = TestConstants.RootAdminEmail,
            password = "WrongPassword123!"
        });

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateToken_Should_Return401_When_EmailDoesNotExist()
    {
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
        request.Headers.Add("tenant", TestConstants.RootTenantId);
        request.Content = JsonContent.Create(new
        {
            email = "nonexistent@example.com",
            password = TestConstants.DefaultPassword
        });

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateToken_Should_Return400_When_EmailIsEmpty()
    {
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
        request.Headers.Add("tenant", TestConstants.RootTenantId);
        request.Content = JsonContent.Create(new
        {
            email = "",
            password = TestConstants.DefaultPassword
        });

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateToken_Should_IncludeCorrectClaimsInJwt_When_LoginSucceeds()
    {
        var token = await _auth.GetRootAdminTokenAsync();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.AccessToken);

        jwt.Issuer.ShouldBe(TestConstants.JwtIssuer);
        jwt.Audiences.ShouldContain(TestConstants.JwtAudience);

        var emailClaim = jwt.Claims.FirstOrDefault(c =>
            c.Type is "email" or System.Security.Claims.ClaimTypes.Email);
        emailClaim.ShouldNotBeNull();
        emailClaim.Value.ShouldBe(TestConstants.RootAdminEmail, StringCompareShould.IgnoreCase);
    }

    [Fact]
    public async Task GenerateToken_Should_Fail_When_TenantHeaderIsMissing()
    {
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
        request.Content = JsonContent.Create(new
        {
            email = TestConstants.RootAdminEmail,
            password = TestConstants.DefaultPassword
        });

        var response = await client.SendAsync(request);

        response.IsSuccessStatusCode.ShouldBeFalse();
    }
}
