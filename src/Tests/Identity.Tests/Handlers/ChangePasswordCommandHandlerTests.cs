using AutoFixture;
using FSH.Framework.Core.Context;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ChangePassword;
using FSH.Modules.Identity.Features.v1.Users.ChangePassword;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Identity.Tests.Handlers;

/// <summary>
/// Tests for ChangePasswordCommandHandler - handles password change operations.
/// </summary>
public sealed class ChangePasswordCommandHandlerTests
{
    private readonly IUserService _userService;
    private readonly ICurrentUser _currentUser;
    private readonly ChangePasswordCommandHandler _sut;
    private readonly IFixture _fixture;

    public ChangePasswordCommandHandlerTests()
    {
        _userService = Substitute.For<IUserService>();
        _currentUser = Substitute.For<ICurrentUser>();
        _sut = new ChangePasswordCommandHandler(_userService, _currentUser);
        _fixture = new Fixture();
    }

    #region Handle - Happy Path Tests

    [Fact]
    public async Task Handle_Should_ReturnSuccessMessage_When_PasswordIsChangedSuccessfully()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            Password = "CurrentPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        var userId = _fixture.Create<Guid>();

        _currentUser.IsAuthenticated().Returns(true);
        _currentUser.GetUserId().Returns(userId);

        _userService.ChangePasswordAsync(command.Password, command.NewPassword, command.ConfirmNewPassword, userId.ToString())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBe("password reset email sent");
    }

    [Fact]
    public async Task Handle_Should_CallUserServiceWithCorrectParameters_When_PasswordChangeIsRequested()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            Password = "OldPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        var userId = _fixture.Create<Guid>();

        _currentUser.IsAuthenticated().Returns(true);
        _currentUser.GetUserId().Returns(userId);

        _userService.ChangePasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _userService.Received(1).ChangePasswordAsync(
            command.Password,
            command.NewPassword,
            command.ConfirmNewPassword,
            userId.ToString());
    }

    #endregion

    #region Handle - Authentication Tests

    [Fact]
    public async Task Handle_Should_ThrowInvalidOperationException_When_UserIsNotAuthenticated()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            Password = "CurrentPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        _currentUser.IsAuthenticated().Returns(false);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        exception.Message.ShouldBe("User is not authenticated.");
    }

    [Fact]
    public async Task Handle_Should_CheckAuthentication_BeforeGettingUserId()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            Password = "CurrentPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        _currentUser.IsAuthenticated().Returns(false);

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        // Assert
        _currentUser.Received(1).IsAuthenticated();
        _currentUser.DidNotReceive().GetUserId();
    }

    #endregion

    #region Handle - Current User Tests

    [Fact]
    public async Task Handle_Should_GetUserIdFromCurrentUser_When_UserIsAuthenticated()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            Password = "CurrentPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        var userId = _fixture.Create<Guid>();

        _currentUser.IsAuthenticated().Returns(true);
        _currentUser.GetUserId().Returns(userId);

        _userService.ChangePasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _currentUser.Received(1).IsAuthenticated();
        _currentUser.Received(1).GetUserId();
    }

    [Fact]
    public async Task Handle_Should_ConvertUserIdToString_When_PassingToUserService()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            Password = "CurrentPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        var userId = _fixture.Create<Guid>();

        _currentUser.IsAuthenticated().Returns(true);
        _currentUser.GetUserId().Returns(userId);

        _userService.ChangePasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _userService.Received(1).ChangePasswordAsync(
            command.Password,
            command.NewPassword,
            command.ConfirmNewPassword,
            userId.ToString());
    }

    #endregion

    #region Handle - Exception Tests

    [Fact]
    public async Task Handle_Should_ThrowException_When_UserServiceThrows()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            Password = "WrongPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        var userId = _fixture.Create<Guid>();

        _currentUser.IsAuthenticated().Returns(true);
        _currentUser.GetUserId().Returns(userId);

        var expectedExceptionMessage = "Current password is incorrect";
        _userService.ChangePasswordAsync(command.Password, command.NewPassword, command.ConfirmNewPassword, userId.ToString())
            .ThrowsAsync(new UnauthorizedAccessException(expectedExceptionMessage));

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        exception.Message.ShouldBe(expectedExceptionMessage);
    }

    [Fact]
    public async Task Handle_Should_ThrowException_When_GetUserIdThrows()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            Password = "CurrentPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        _currentUser.IsAuthenticated().Returns(true);
        _currentUser.GetUserId().Throws(new InvalidOperationException("User ID not available"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _sut.Handle(command, CancellationToken.None));
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
    public async Task Handle_Should_HandleCancellationToken_Properly()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            Password = "CurrentPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        var userId = _fixture.Create<Guid>();
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _currentUser.IsAuthenticated().Returns(true);
        _currentUser.GetUserId().Returns(userId);

        _userService.ChangePasswordAsync(command.Password, command.NewPassword, command.ConfirmNewPassword, userId.ToString())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, cancellationToken);

        // Assert
        result.ShouldBe("password reset email sent");
    }

    #endregion

    #region Handle - Edge Cases

    [Fact]
    public async Task Handle_Should_HandleEmptyPasswords_When_ProvidedInCommand()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            Password = "",
            NewPassword = "",
            ConfirmNewPassword = ""
        };

        var userId = _fixture.Create<Guid>();

        _currentUser.IsAuthenticated().Returns(true);
        _currentUser.GetUserId().Returns(userId);

        _userService.ChangePasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBe("password reset email sent");
        await _userService.Received(1).ChangePasswordAsync("", "", "", userId.ToString());
    }

    #endregion
}