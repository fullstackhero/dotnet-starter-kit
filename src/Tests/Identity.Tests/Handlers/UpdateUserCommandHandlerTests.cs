using AutoFixture;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.UpdateUser;
using FSH.Modules.Identity.Features.v1.Users.UpdateUser;
using NSubstitute;
using Shouldly;
using Xunit;
using Mediator;

namespace Identity.Tests.Handlers;

public sealed class UpdateUserCommandHandlerTests
{
    private readonly IUserService _userService;
    private readonly UpdateUserCommandHandler _sut;
    private readonly IFixture _fixture;

    public UpdateUserCommandHandlerTests()
    {
        _userService = Substitute.For<IUserService>();
        _sut = new UpdateUserCommandHandler(_userService);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task Handle_Should_CallUpdateAsync_WithCorrectParameters()
    {
        // Arrange
        var command = _fixture.Create<UpdateUserCommand>();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBe(Unit.Value);
        await _userService.Received(1).UpdateAsync(
            command.Id,
            command.FirstName ?? string.Empty,
            command.LastName ?? string.Empty,
            command.PhoneNumber ?? string.Empty,
            command.Image!,
            command.DeleteCurrentImage,
            command.Locale);
    }

    [Fact]
    public async Task Handle_Should_HandleNullOptionalFields_WithEmptyStrings()
    {
        // Arrange
        var command = new UpdateUserCommand
        {
            Id = _fixture.Create<string>(),
            FirstName = null,
            LastName = null,
            PhoneNumber = null,
            Image = null,
            DeleteCurrentImage = true
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _userService.Received(1).UpdateAsync(
            command.Id,
            string.Empty,
            string.Empty,
            string.Empty,
            null!,
            true,
            command.Locale);
    }

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_CommandIsNull()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ThrowException_When_UserServiceThrows()
    {
        // Arrange
        var command = _fixture.Create<UpdateUserCommand>();
        var expectedExceptionMessage = "Update failed";
        _userService.UpdateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<FSH.Framework.Shared.Storage.FileUploadRequest>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(x => throw new InvalidOperationException(expectedExceptionMessage));

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        exception.Message.ShouldBe(expectedExceptionMessage);
    }
}
