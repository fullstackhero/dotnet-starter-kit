using AutoFixture;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ToggleUserStatus;
using FSH.Modules.Identity.Features.v1.Users.ToggleUserStatus;
using NSubstitute;
using Shouldly;
using Xunit;
using Mediator;

namespace Identity.Tests.Handlers;

public sealed class ToggleUserStatusCommandHandlerTests
{
    private readonly IUserService _userService;
    private readonly ToggleUserStatusCommandHandler _sut;
    private readonly IFixture _fixture;

    public ToggleUserStatusCommandHandlerTests()
    {
        _userService = Substitute.For<IUserService>();
        _sut = new ToggleUserStatusCommandHandler(_userService);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task Handle_Should_CallToggleStatusAsync_WithCorrectParameters()
    {
        // Arrange
        var command = new ToggleUserStatusCommand { UserId = _fixture.Create<string>(), ActivateUser = true };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBe(Unit.Value);
        await _userService.Received(1).ToggleStatusAsync(true, command.UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_CommandIsNull()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowArgumentException_When_UserIdIsEmpty()
    {
        // Arrange
        var command = new ToggleUserStatusCommand { UserId = "", ActivateUser = true };

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(async () =>
            await _sut.Handle(command, CancellationToken.None));
        
        exception.ParamName.ShouldBe("UserId");
    }

    [Fact]
    public async Task Handle_Should_PassCancellationToken_ToUserService()
    {
        // Arrange
        var command = new ToggleUserStatusCommand { UserId = _fixture.Create<string>(), ActivateUser = false };
        using var cts = new CancellationTokenSource();

        // Act
        await _sut.Handle(command, cts.Token);

        // Assert
        await _userService.Received(1).ToggleStatusAsync(false, command.UserId, cts.Token);
    }
}
