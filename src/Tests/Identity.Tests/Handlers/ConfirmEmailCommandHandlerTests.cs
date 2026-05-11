using AutoFixture;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ConfirmEmail;
using FSH.Modules.Identity.Features.v1.Users.ConfirmEmail;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Identity.Tests.Handlers;

public sealed class ConfirmEmailCommandHandlerTests
{
    private readonly IUserService _userService;
    private readonly ConfirmEmailCommandHandler _sut;
    private readonly IFixture _fixture;

    public ConfirmEmailCommandHandlerTests()
    {
        _userService = Substitute.For<IUserService>();
        _sut = new ConfirmEmailCommandHandler(_userService);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task Handle_Should_CallConfirmEmailAsync_WithCorrectParameters()
    {
        // Arrange
        var command = _fixture.Create<ConfirmEmailCommand>();
        var expectedResponse = "Email confirmed.";
        _userService.ConfirmEmailAsync(command.UserId, command.Code, command.Tenant, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedResponse);
        await _userService.Received(1).ConfirmEmailAsync(command.UserId, command.Code, command.Tenant, Arg.Any<CancellationToken>());
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
        var command = _fixture.Create<ConfirmEmailCommand>();
        using var cts = new CancellationTokenSource();

        // Act
        await _sut.Handle(command, cts.Token);

        // Assert
        await _userService.Received(1).ConfirmEmailAsync(command.UserId, command.Code, command.Tenant, cts.Token);
    }
}
