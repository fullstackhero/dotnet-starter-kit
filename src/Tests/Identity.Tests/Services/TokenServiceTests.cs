using System.Diagnostics.Metrics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FSH.Modules.Identity;
using FSH.Modules.Identity.Authorization.Jwt;
using FSH.Modules.Identity.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace Identity.Tests.Services;

/// <summary>
/// Tests for TokenService - issues JWT access tokens and opaque refresh tokens.
/// </summary>
public sealed class TokenServiceTests : IDisposable
{
    private const string SigningKey = "this-is-a-very-long-signing-key-32!!";
    private const string Issuer = "fsh-issuer";
    private const string Audience = "fsh-audience";

    private static readonly DateTimeOffset FixedNow =
        new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly ILogger<TokenService> _logger;
    private readonly IdentityMetrics _metrics;

    public TokenServiceTests()
    {
        _logger = Substitute.For<ILogger<TokenService>>();

        // IdentityMetrics only needs a Meter; return a real one from a stubbed factory.
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(_ => new Meter(IdentityMetrics.MeterName));
        _metrics = new IdentityMetrics(meterFactory);
    }

    private TokenService CreateService(
        int accessTokenMinutes = 30,
        int refreshTokenDays = 7)
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = SigningKey,
            AccessTokenMinutes = accessTokenMinutes,
            RefreshTokenDays = refreshTokenDays
        });

        var timeProvider = new FixedTimeProvider(FixedNow);
        return new TokenService(options, _logger, _metrics, timeProvider);
    }

    private static Claim[] SampleClaims() =>
        new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Email, "user@example.com")
        };

    private static JwtSecurityToken ReadToken(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    #region IssueAsync Tests

    [Fact]
    public async Task IssueAsync_Should_ReturnTokenResponseWithAllFields()
    {
        // Arrange
        var service = CreateService();

        // Act
        var response = await service.IssueAsync("user-123", SampleClaims());

        // Assert
        response.ShouldNotBeNull();
        response.AccessToken.ShouldNotBeNullOrWhiteSpace();
        response.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        response.AccessTokenExpiresAt.ShouldBe(FixedNow.UtcDateTime.AddMinutes(30));
        response.RefreshTokenExpiresAt.ShouldBe(FixedNow.UtcDateTime.AddDays(7));
    }

    [Fact]
    public async Task IssueAsync_Should_RespectConfiguredLifetimes()
    {
        // Arrange
        var service = CreateService(accessTokenMinutes: 5, refreshTokenDays: 30);

        // Act
        var response = await service.IssueAsync("user-123", SampleClaims());

        // Assert
        response.AccessTokenExpiresAt.ShouldBe(FixedNow.UtcDateTime.AddMinutes(5));
        response.RefreshTokenExpiresAt.ShouldBe(FixedNow.UtcDateTime.AddDays(30));
    }

    [Fact]
    public async Task IssueAsync_Should_ProduceTokenWithIssuerAudienceAndClaims()
    {
        // Arrange
        var service = CreateService();

        // Act
        var response = await service.IssueAsync("user-123", SampleClaims());
        var jwt = ReadToken(response.AccessToken);

        // Assert
        jwt.Issuer.ShouldBe(Issuer);
        jwt.Audiences.ShouldContain(Audience);
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user-123");
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.Email && c.Value == "user@example.com");
    }

    [Fact]
    public async Task IssueAsync_Should_ProduceUniqueRefreshTokens()
    {
        // Arrange
        var service = CreateService();

        // Act
        var first = await service.IssueAsync("user-123", SampleClaims());
        var second = await service.IssueAsync("user-123", SampleClaims());

        // Assert - refresh token is a random GUID, so two issues must differ
        first.RefreshToken.ShouldNotBe(second.RefreshToken);
    }

    [Fact]
    public async Task IssueAsync_Should_ProduceTokenSignedWithConfiguredKey()
    {
        // Arrange - use the system clock so the issued token is valid against full validation (incl. lifetime)
        var options = Options.Create(new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = SigningKey,
            AccessTokenMinutes = 30,
            RefreshTokenDays = 7
        });
        var service = new TokenService(options, _logger, _metrics, TimeProvider.System);

        // Act
        var response = await service.IssueAsync("user-123", SampleClaims());

        // Assert - signature, issuer and audience must validate against the configured signing key
        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        var validationResult = await handler.ValidateTokenAsync(response.AccessToken, validationParameters);

        validationResult.IsValid.ShouldBeTrue();
    }

    #endregion

    #region IssueAccessOnlyAsync Tests

    [Fact]
    public async Task IssueAccessOnlyAsync_Should_UseConfiguredLifetime_When_NoOverride()
    {
        // Arrange
        var service = CreateService(accessTokenMinutes: 30);

        // Act
        var (accessToken, expiresAt) = await service.IssueAccessOnlyAsync("user-123", SampleClaims());

        // Assert
        accessToken.ShouldNotBeNullOrWhiteSpace();
        expiresAt.ShouldBe(FixedNow.UtcDateTime.AddMinutes(30));
    }

    [Fact]
    public async Task IssueAccessOnlyAsync_Should_UseSuppliedLifetime_When_Overridden()
    {
        // Arrange - caller-supplied lifetime wins over configured AccessTokenMinutes
        var service = CreateService(accessTokenMinutes: 30);
        var lifetime = TimeSpan.FromMinutes(2);

        // Act
        var (_, expiresAt) = await service.IssueAccessOnlyAsync("user-123", SampleClaims(), lifetime);

        // Assert
        expiresAt.ShouldBe(FixedNow.UtcDateTime.Add(lifetime));
    }

    [Fact]
    public async Task IssueAccessOnlyAsync_Should_EmbedSuppliedClaims()
    {
        // Arrange
        var service = CreateService();

        // Act
        var (accessToken, _) = await service.IssueAccessOnlyAsync("user-123", SampleClaims());
        var jwt = ReadToken(accessToken);

        // Assert
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user-123");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_OptionsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TokenService(null!, _logger, _metrics, TimeProvider.System));
    }

    #endregion

    public void Dispose() => _metrics.Dispose();

    /// <summary>
    /// Minimal TimeProvider that always reports a fixed instant, so token expiry math is deterministic.
    /// </summary>
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
