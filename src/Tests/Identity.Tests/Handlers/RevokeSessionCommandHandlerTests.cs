using AutoFixture;
using FSH.Framework.Core.Context;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Sessions.RevokeSession;
using FSH.Modules.Identity.Features.v1.Sessions.RevokeSession;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Identity.Tests.Handlers;

/// <summary>
/// Tests for RevokeSessionCommandHandler - handles session revocation.
/// </summary>
public sealed class RevokeSessionCommandHandlerTests
{
    private readonly ISessionService _sessionService;
    private readonly ICurrentUser _currentUser;
    private readonly RevokeSessionCommandHandler _sut;
    private readonly IFixture _fixture;

    public RevokeSessionCommandHandlerTests()
    {
        _sessionService = Substitute.For<ISessionService>();
        _currentUser = Substitute.For<ICurrentUser>();
        _sut = new RevokeSessionCommandHandler(_sessionService, _currentUser);
        _fixture = new Fixture();
    }

    #region Handle - Happy Path Tests

    [Fact]
    public async Task Handle_Should_ReturnTrue_When_SessionIsSuccessfullyRevoked()
    {
        // Arrange
        var sessionId = _fixture.Create<Guid>();
        var command = new RevokeSessionCommand(sessionId);
        var userId = _fixture.Create<Guid>();

        _currentUser.GetUserId().Returns(userId);
        _sessionService.RevokeSessionAsync(sessionId, userId.ToString(), "User requested", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnFalse_When_SessionRevocationFails()
    {
        // Arrange
        var sessionId = _fixture.Create<Guid>();
        var command = new RevokeSessionCommand(sessionId);
        var userId = _fixture.Create<Guid>();

        _currentUser.GetUserId().Returns(userId);
        _sessionService.RevokeSessionAsync(sessionId, userId.ToString(), "User requested", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_Should_CallSessionServiceWithCorrectParameters()
    {
        // Arrange
        var sessionId = _fixture.Create<Guid>();
        var command = new RevokeSessionCommand(sessionId);
        var userId = _fixture.Create<Guid>();

        _currentUser.GetUserId().Returns(userId);
        _sessionService.RevokeSessionAsync(sessionId, userId.ToString(), "User requested", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _sessionService.Received(1).RevokeSessionAsync(
            sessionId,
            userId.ToString(),
            "User requested",
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Current User Tests

    [Fact]
    public async Task Handle_Should_GetUserIdFromCurrentUser()
    {
        // Arrange
        var sessionId = _fixture.Create<Guid>();
        var command = new RevokeSessionCommand(sessionId);
        var userId = _fixture.Create<Guid>();

        _currentUser.GetUserId().Returns(userId);
        _sessionService.RevokeSessionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _currentUser.Received(1).GetUserId();
    }

    [Fact]
    public async Task Handle_Should_ConvertUserIdToString_When_PassingToSessionService()
    {
        // Arrange
        var sessionId = _fixture.Create<Guid>();
        var command = new RevokeSessionCommand(sessionId);
        var userId = _fixture.Create<Guid>();

        _currentUser.GetUserId().Returns(userId);
        _sessionService.RevokeSessionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _sessionService.Received(1).RevokeSessionAsync(
            sessionId,
            userId.ToString(),
            "User requested",
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Exception Tests

    [Fact]
    public async Task Handle_Should_ThrowException_When_SessionServiceThrows()
    {
        // Arrange
        var sessionId = _fixture.Create<Guid>();
        var command = new RevokeSessionCommand(sessionId);
        var userId = _fixture.Create<Guid>();

        _currentUser.GetUserId().Returns(userId);

        var expectedExceptionMessage = "Session not found";
        _sessionService.RevokeSessionAsync(sessionId, userId.ToString(), "User requested", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(expectedExceptionMessage));

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        exception.Message.ShouldBe(expectedExceptionMessage);
    }

    [Fact]
    public async Task Handle_Should_ThrowException_When_CurrentUserThrows()
    {
        // Arrange
        var sessionId = _fixture.Create<Guid>();
        var command = new RevokeSessionCommand(sessionId);

        _currentUser.GetUserId().Throws(new UnauthorizedAccessException("User not authenticated"));

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            async () => await _sut.Handle(command, CancellationToken.None));
    }

    #endregion

    #region Handle - CancellationToken Tests

    [Fact]
    public async Task Handle_Should_PassCancellationToken_ToSessionService()
    {
        // Arrange
        var sessionId = _fixture.Create<Guid>();
        var command = new RevokeSessionCommand(sessionId);
        var userId = _fixture.Create<Guid>();
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _currentUser.GetUserId().Returns(userId);
        _sessionService.RevokeSessionAsync(sessionId, userId.ToString(), "User requested", cancellationToken)
            .Returns(true);

        // Act
        await _sut.Handle(command, cancellationToken);

        // Assert
        await _sessionService.Received(1).RevokeSessionAsync(
            sessionId,
            userId.ToString(),
            "User requested",
            cancellationToken);
    }

    #endregion

    #region Handle - Edge Cases

    [Fact]
    public async Task Handle_Should_HandleEmptyGuid_When_SessionIdIsEmpty()
    {
        // Arrange
        var sessionId = Guid.Empty;
        var command = new RevokeSessionCommand(sessionId);
        var userId = _fixture.Create<Guid>();

        _currentUser.GetUserId().Returns(userId);
        _sessionService.RevokeSessionAsync(sessionId, userId.ToString(), "User requested", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        await _sessionService.Received(1).RevokeSessionAsync(
            Guid.Empty,
            userId.ToString(),
            "User requested",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_UseFixedReason_When_RevokingSession()
    {
        // Arrange
        var sessionId = _fixture.Create<Guid>();
        var command = new RevokeSessionCommand(sessionId);
        var userId = _fixture.Create<Guid>();

        _currentUser.GetUserId().Returns(userId);
        _sessionService.RevokeSessionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _sessionService.Received(1).RevokeSessionAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            "User requested",
            Arg.Any<CancellationToken>());
    }

    #endregion
}