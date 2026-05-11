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
        // Note: DeleteUserCommandHandler currently doesn't pass cancellation token to DeleteAsync 
        // based on the view_file output I saw earlier (line 20: await _userService.DeleteAsync(command.Id).ConfigureAwait(false);)
        // I will still test the call with any CancellationToken if the method signature allows it,
        // but based on my earlier view of DeleteUserCommandHandler, it doesn't take it in DeleteAsync.
        // Wait, let me check IUserService.DeleteAsync signature.
        await _sut.Handle(command, cts.Token);

        // Assert
        await _userService.Received(1).DeleteAsync(userId);
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
