using AutoFixture;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.DeleteUser;
using FSH.Modules.Identity.Features.v1.Users.DeleteUser;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Identity.Tests.Handlers;

public sealed class DeleteUserCommandHandlerTests
{
    private readonly IUserService _userService;
    private readonly DeleteUserCommandHandler _sut;
    private readonly IFixture _fixture;

    public DeleteUserCommandHandlerTests()
    {
        _userService = Substitute.For<IUserService>();
        _sut = new DeleteUserCommandHandler(_userService);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task Handle_Should_CallDeleteAsync_WithCorrectUserId()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var command = new DeleteUserCommand(userId);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _userService.Received(1).DeleteAsync(userId);
    }

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_CommandIsNull()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_PassCancellationToken_ToUserService()
    {
        // Arrange
        var userId = _fixture.Create<string>();
        var command = new DeleteUserCommand(userId);
        using var cts = new CancellationTokenSource();

        // Act
        await _sut.Handle(command, cts.Token);

        // Assert — the handler must forward the request's token, not the default.
        await _userService.Received(1).DeleteAsync(userId, cts.Token);
    }

    [Fact]
    public async Task Handle_Should_ThrowException_When_UserServiceThrows()
    {
        // Arrange
        var command = _fixture.Create<DeleteUserCommand>();
        var expectedExceptionMessage = "User not found";
        _userService.DeleteAsync(Arg.Any<string>())
            .Returns(x => throw new InvalidOperationException(expectedExceptionMessage));

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        exception.Message.ShouldBe(expectedExceptionMessage);
    }
}
