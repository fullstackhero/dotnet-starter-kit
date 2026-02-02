using AutoFixture;
using FSH.Framework.Core.Context;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Tokens.RefreshToken;
using FSH.Modules.Identity.Features.v1.Tokens.RefreshToken;
using NSubstitute;
using System.Security.Claims;

namespace Identity.Tests.Handlers;

/// <summary>
/// Tests for RefreshTokenCommandHandler - handles token refresh flow.
/// </summary>
public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ISecurityAudit _securityAudit;
    private readonly IRequestContext _requestContext;
    private readonly ISessionService _sessionService;
    private readonly RefreshTokenCommandHandler _sut;
    private readonly IFixture _fixture;

    public RefreshTokenCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _tokenService = Substitute.For<ITokenService>();
        _securityAudit = Substitute.For<ISecurityAudit>();
        _requestContext = Substitute.For<IRequestContext>();
        _sessionService = Substitute.For<ISessionService>();

        _sut = new RefreshTokenCommandHandler(
            _identityService,
            _tokenService,
            _securityAudit,
            _requestContext,
            _sessionService);

        _fixture = new Fixture();
    }

    #region Handle - Happy Path Tests

    [Fact]
    public async Task Handle_Should_ReturnNewTokens_When_RefreshTokenIsValid()
    {
        // Arrange
        var oldAccessToken = CreateValidJwtToken("user123", "test@example.com");
        var command = new RefreshTokenCommand(oldAccessToken, "valid-refresh-token");
        
        var userId = "user123";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "TestUser"),
            new(ClaimTypes.Email, "test@example.com")
        };

        var newToken = new TokenResponse(
            AccessToken: _fixture.Create<string>(),
            RefreshToken: _fixture.Create<string>(),
            RefreshTokenExpiresAt: DateTime.UtcNow.AddDays(7),
            AccessTokenExpiresAt: DateTime.UtcNow.AddHours(1));

        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns((userId, claims));

        _sessionService.ValidateSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _tokenService.IssueAsync(userId, claims, null, Arg.Any<CancellationToken>())
            .Returns(newToken);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Token.ShouldBe(newToken.AccessToken);
        result.RefreshToken.ShouldBe(newToken.RefreshToken);
        result.RefreshTokenExpiryTime.ShouldBe(newToken.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task Handle_Should_CallAllServicesWithCorrectParameters_When_RefreshTokenIsValid()
    {
        // Arrange
        var command = new RefreshTokenCommand("access-token", "refresh-token");
        var userId = _fixture.Create<string>();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var newToken = _fixture.Create<TokenResponse>();

        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns((userId, claims));

        _sessionService.ValidateSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _tokenService.IssueAsync(userId, claims, null, Arg.Any<CancellationToken>())
            .Returns(newToken);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _identityService.Received(1).ValidateRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>());
        await _sessionService.Received(1).ValidateSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _tokenService.Received(1).IssueAsync(userId, claims, null, Arg.Any<CancellationToken>());
        await _identityService.Received(1).StoreRefreshTokenAsync(userId, newToken.RefreshToken, newToken.RefreshTokenExpiresAt, Arg.Any<CancellationToken>());
        await _sessionService.Received(1).UpdateSessionRefreshTokenAsync(Arg.Any<string>(), Arg.Any<string>(), newToken.RefreshTokenExpiresAt, Arg.Any<CancellationToken>());
        await _securityAudit.Received(1).TokenRevokedAsync(userId, "test-client", "RefreshTokenRotated", Arg.Any<CancellationToken>());
        await _securityAudit.Received(1).TokenIssuedAsync(userId, Arg.Any<string>(), "test-client", Arg.Any<string>(), newToken.AccessTokenExpiresAt, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Invalid Refresh Token Tests

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_RefreshTokenIsInvalid()
    {
        // Arrange
        var command = new RefreshTokenCommand("access-token", "invalid-refresh-token");

        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns((ValueTuple<string, IReadOnlyList<Claim>>?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        exception.Message.ShouldBe("Invalid refresh token.");
    }

    [Fact]
    public async Task Handle_Should_AuditTokenRevocation_When_RefreshTokenIsInvalid()
    {
        // Arrange
        var command = new RefreshTokenCommand("access-token", "invalid-refresh-token");

        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns((ValueTuple<string, IReadOnlyList<Claim>>?)null);

        // Act
        await Should.ThrowAsync<UnauthorizedAccessException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        // Assert
        await _securityAudit.Received(1).TokenRevokedAsync("unknown", "test-client", "InvalidRefreshToken", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Session Validation Tests

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_SessionIsRevoked()
    {
        // Arrange
        var command = new RefreshTokenCommand("access-token", "valid-refresh-token");
        var userId = _fixture.Create<string>();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };

        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns((userId, claims));

        _sessionService.ValidateSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        exception.Message.ShouldBe("Session has been revoked.");
    }

    [Fact]
    public async Task Handle_Should_AuditSessionRevocation_When_SessionIsRevoked()
    {
        // Arrange
        var command = new RefreshTokenCommand("access-token", "valid-refresh-token");
        var userId = _fixture.Create<string>();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };

        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns((userId, claims));

        _sessionService.ValidateSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await Should.ThrowAsync<UnauthorizedAccessException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        // Assert
        await _securityAudit.Received(1).TokenRevokedAsync(userId, "test-client", "SessionRevoked", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Access Token Subject Mismatch Tests

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_AccessTokenSubjectMismatch()
    {
        // Arrange
        var wrongAccessToken = CreateValidJwtToken("different-user", "other@example.com");
        var command = new RefreshTokenCommand(wrongAccessToken, "valid-refresh-token");
        var userId = "original-user";
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };

        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateRefreshTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns((userId, claims));

        _sessionService.ValidateSessionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        exception.Message.ShouldBe("Access token subject mismatch.");
    }

    #endregion

    #region Handle - Null Command Tests

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_CommandIsNull()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.Handle(null!, CancellationToken.None));
    }

    #endregion

    #region Handle - CancellationToken Tests

    [Fact]
    public async Task Handle_Should_PassCancellationToken_ToAllServices()
    {
        // Arrange
        var command = new RefreshTokenCommand("access-token", "refresh-token");
        var userId = _fixture.Create<string>();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var newToken = _fixture.Create<TokenResponse>();
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateRefreshTokenAsync(command.RefreshToken, cancellationToken)
            .Returns((userId, claims));

        _sessionService.ValidateSessionAsync(Arg.Any<string>(), cancellationToken)
            .Returns(true);

        _tokenService.IssueAsync(userId, claims, null, cancellationToken)
            .Returns(newToken);

        // Act
        await _sut.Handle(command, cancellationToken);

        // Assert
        await _identityService.Received(1).ValidateRefreshTokenAsync(command.RefreshToken, cancellationToken);
        await _sessionService.Received(1).ValidateSessionAsync(Arg.Any<string>(), cancellationToken);
        await _tokenService.Received(1).IssueAsync(userId, claims, null, cancellationToken);
        await _identityService.Received(1).StoreRefreshTokenAsync(userId, newToken.RefreshToken, newToken.RefreshTokenExpiresAt, cancellationToken);
        await _sessionService.Received(1).UpdateSessionRefreshTokenAsync(Arg.Any<string>(), Arg.Any<string>(), newToken.RefreshTokenExpiresAt, cancellationToken);
    }

    #endregion

    #region Helper Methods

    private static string CreateValidJwtToken(string userId, string email)
    {
        // Create a simple JWT-like token for testing purposes
        // This is just for parsing tests, not a real JWT
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            $"{{\"sub\":\"{userId}\",\"email\":\"{email}\",\"exp\":{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}}}"));
        var signature = "fake-signature";
        return $"{header}.{payload}.{signature}";
    }

    #endregion
}