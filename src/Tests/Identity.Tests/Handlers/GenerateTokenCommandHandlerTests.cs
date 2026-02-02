using AutoFixture;
using FSH.Framework.Core.Context;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Security.Claims;

namespace Identity.Tests.Handlers;

/// <summary>
/// Tests for GenerateTokenCommandHandler - handles user login and token generation.
/// </summary>
public sealed class GenerateTokenCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ISecurityAudit _securityAudit;
    private readonly IRequestContext _requestContext;
    private readonly IOutboxStore _outboxStore;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;
    private readonly ISessionService _sessionService;
    private readonly ILogger<GenerateTokenCommandHandler> _logger;
    private readonly GenerateTokenCommandHandler _sut;
    private readonly IFixture _fixture;

    public GenerateTokenCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _tokenService = Substitute.For<ITokenService>();
        _securityAudit = Substitute.For<ISecurityAudit>();
        _requestContext = Substitute.For<IRequestContext>();
        _outboxStore = Substitute.For<IOutboxStore>();
        _multiTenantContextAccessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        _sessionService = Substitute.For<ISessionService>();
        _logger = Substitute.For<ILogger<GenerateTokenCommandHandler>>();

        _sut = new GenerateTokenCommandHandler(
            _identityService,
            _tokenService,
            _securityAudit,
            _requestContext,
            _outboxStore,
            _multiTenantContextAccessor,
            _sessionService,
            _logger);

        _fixture = new Fixture();
    }

    #region Handle - Happy Path Tests

    [Fact]
    public async Task Handle_Should_ReturnTokenResponse_When_CredentialsAreValid()
    {
        // Arrange
        var command = new GenerateTokenCommand("user@example.com", "password123");
        var userId = _fixture.Create<string>();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "TestUser"),
            new(ClaimTypes.Email, command.Email)
        };
        var expectedToken = new TokenResponse(
            AccessToken: _fixture.Create<string>(),
            RefreshToken: _fixture.Create<string>(),
            RefreshTokenExpiresAt: DateTime.UtcNow.AddDays(7),
            AccessTokenExpiresAt: DateTime.UtcNow.AddHours(1));

        _requestContext.IpAddress.Returns("192.168.1.1");
        _requestContext.UserAgent.Returns("TestAgent");
        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateCredentialsAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns((userId, claims));

        _tokenService.IssueAsync(userId, claims, null, Arg.Any<CancellationToken>())
            .Returns(expectedToken);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldBe(expectedToken.AccessToken);
        result.RefreshToken.ShouldBe(expectedToken.RefreshToken);
        result.RefreshTokenExpiresAt.ShouldBe(expectedToken.RefreshTokenExpiresAt);
        result.AccessTokenExpiresAt.ShouldBe(expectedToken.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Handle_Should_CallAllServicesWithCorrectParameters_When_CredentialsAreValid()
    {
        // Arrange
        var command = new GenerateTokenCommand("user@example.com", "password123");
        var userId = _fixture.Create<string>();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "TestUser")
        };
        var token = _fixture.Create<TokenResponse>();

        _requestContext.IpAddress.Returns("192.168.1.1");
        _requestContext.UserAgent.Returns("TestAgent");
        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateCredentialsAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns((userId, claims));

        _tokenService.IssueAsync(userId, claims, null, Arg.Any<CancellationToken>())
            .Returns(token);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _identityService.Received(1).ValidateCredentialsAsync(command.Email, command.Password, Arg.Any<CancellationToken>());
        await _tokenService.Received(1).IssueAsync(userId, claims, null, Arg.Any<CancellationToken>());
        await _identityService.Received(1).StoreRefreshTokenAsync(userId, token.RefreshToken, token.RefreshTokenExpiresAt, Arg.Any<CancellationToken>());
        await _securityAudit.Received(1).LoginSucceededAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _securityAudit.Received(1).TokenIssuedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _outboxStore.Received(1).AddAsync(Arg.Any<FSH.Framework.Eventing.Abstractions.IIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Invalid Credentials Tests

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_CredentialsAreInvalid()
    {
        // Arrange
        var command = new GenerateTokenCommand("user@example.com", "wrongpassword");
        
        _requestContext.IpAddress.Returns("192.168.1.1");
        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateCredentialsAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns((ValueTuple<string, IReadOnlyList<Claim>>?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        exception.Message.ShouldBe("Invalid credentials.");
    }

    [Fact]
    public async Task Handle_Should_AuditFailedLogin_When_CredentialsAreInvalid()
    {
        // Arrange
        var command = new GenerateTokenCommand("user@example.com", "wrongpassword");
        
        _requestContext.IpAddress.Returns("192.168.1.1");
        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateCredentialsAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns((ValueTuple<string, IReadOnlyList<Claim>>?)null);

        // Act
        await Should.ThrowAsync<UnauthorizedAccessException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        // Assert
        await _securityAudit.Received(1).LoginFailedAsync(
            command.Email,
            "test-client",
            "InvalidCredentials",
            "192.168.1.1",
            Arg.Any<CancellationToken>());
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
        var command = new GenerateTokenCommand("user@example.com", "password123");
        var userId = _fixture.Create<string>();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var token = _fixture.Create<TokenResponse>();
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _requestContext.IpAddress.Returns("192.168.1.1");
        _requestContext.UserAgent.Returns("TestAgent");
        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateCredentialsAsync(command.Email, command.Password, cancellationToken)
            .Returns((userId, claims));

        _tokenService.IssueAsync(userId, claims, null, cancellationToken)
            .Returns(token);

        // Act
        await _sut.Handle(command, cancellationToken);

        // Assert
        await _identityService.Received(1).ValidateCredentialsAsync(command.Email, command.Password, cancellationToken);
        await _tokenService.Received(1).IssueAsync(userId, claims, null, cancellationToken);
        await _identityService.Received(1).StoreRefreshTokenAsync(userId, token.RefreshToken, token.RefreshTokenExpiresAt, cancellationToken);
        await _outboxStore.Received(1).AddAsync(Arg.Any<FSH.Framework.Eventing.Abstractions.IIntegrationEvent>(), cancellationToken);
    }

    #endregion

    #region Handle - Session Creation Exception Tests

    [Fact]
    public async Task Handle_Should_ContinueSuccessfully_When_SessionCreationFails()
    {
        // Arrange
        var command = new GenerateTokenCommand("user@example.com", "password123");
        var userId = _fixture.Create<string>();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var token = _fixture.Create<TokenResponse>();

        _requestContext.IpAddress.Returns("192.168.1.1");
        _requestContext.UserAgent.Returns("TestAgent");
        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateCredentialsAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns((userId, claims));

        _tokenService.IssueAsync(userId, claims, null, Arg.Any<CancellationToken>())
            .Returns(token);

        _sessionService.CreateSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database not available"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldBe(token.AccessToken);
    }

    #endregion

    #region Handle - Request Context Tests

    [Fact]
    public async Task Handle_Should_HandleMissingRequestContextValues()
    {
        // Arrange
        var command = new GenerateTokenCommand("user@example.com", "password123");
        var userId = _fixture.Create<string>();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var token = _fixture.Create<TokenResponse>();

        // Request context returns null values
        _requestContext.IpAddress.Returns((string?)null);
        _requestContext.UserAgent.Returns((string?)null);
        _requestContext.ClientId.Returns("test-client");

        _identityService.ValidateCredentialsAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns((userId, claims));

        _tokenService.IssueAsync(userId, claims, null, Arg.Any<CancellationToken>())
            .Returns(token);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        await _securityAudit.Received().LoginSucceededAsync(
            userId,
            Arg.Any<string>(),
            "test-client",
            "unknown", // IP should default to "unknown"
            "unknown", // UserAgent should default to "unknown"
            Arg.Any<CancellationToken>());
    }

    #endregion
}