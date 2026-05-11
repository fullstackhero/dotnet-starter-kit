using AutoFixture;
using FSH.Framework.Web.Origin;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ForgotPassword;
using FSH.Modules.Identity.Features.v1.Users.ForgotPassword;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Identity.Tests.Handlers;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly IUserService _userService;
    private readonly IOptions<OriginOptions> _originOptions;
    private readonly ForgotPasswordCommandHandler _sut;
    private readonly IFixture _fixture;

    public ForgotPasswordCommandHandlerTests()
    {
        _userService = Substitute.For<IUserService>();
        _originOptions = Substitute.For<IOptions<OriginOptions>>();
        _sut = new ForgotPasswordCommandHandler(_userService, _originOptions);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task Handle_Should_CallForgotPasswordAsync_When_ValidRequest()
    {
        // Arrange
        var command = _fixture.Create<ForgotPasswordCommand>();
        var originUrl = "https://test.com";
        _originOptions.Value.Returns(new OriginOptions { OriginUrl = new Uri(originUrl) });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBe("Password reset email sent.");
        await _userService.Received(1).ForgotPasswordAsync(command.Email, Arg.Is<string>(s => s.StartsWith(originUrl)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowInvalidOperationException_When_OriginNotConfigured()
    {
        // Arrange
        var command = _fixture.Create<ForgotPasswordCommand>();
        _originOptions.Value.Returns(new OriginOptions { OriginUrl = null });

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sut.Handle(command, CancellationToken.None));
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
        var command = _fixture.Create<ForgotPasswordCommand>();
        var originUrl = "https://test.com";
        _originOptions.Value.Returns(new OriginOptions { OriginUrl = new Uri(originUrl) });
        using var cts = new CancellationTokenSource();

        // Act
        await _sut.Handle(command, cts.Token);

        // Assert
        await _userService.Received(1).ForgotPasswordAsync(command.Email, Arg.Is<string>(s => s.StartsWith(originUrl)), cts.Token);
    }
}
