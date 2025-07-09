using FSH.Starter.WebApi.Host;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Xunit;
using MediatR;
using FSH.Framework.Core.Auth.Features.PasswordReset;
using FluentAssertions;
using FSH.Starter.WebApi.Contracts.Common;

namespace FSH.Starter.Tests.Unit.Controllers;

public class AuthControllerTests_ForgotPassword
{
    private readonly IMediator _mediator;
    private readonly AuthController _controller;

    public AuthControllerTests_ForgotPassword()
    {
        _mediator = Substitute.For<IMediator>();
        _controller = new AuthController(_mediator);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithValidTckn_ShouldReturnOkAsync()
    {
        // Arrange
        var command = new ForgotPasswordCommand 
        { 
            TcknOrMemberNumber = "12345678901",
            BirthDate = new DateTime(1990, 1, 1)
        };
        var response = new ForgotPasswordResponse { Message = "Password reset options sent." };
        _mediator.Send(Arg.Is<ForgotPasswordCommand>(c => c.TcknOrMemberNumber == command.TcknOrMemberNumber), default).Returns(Task.FromResult(response));

        // Act
        var result = await _controller.ForgotPasswordAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<ForgotPasswordResponse>>(okResult.Value);
        apiResponse.Should().NotBeNull();
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithInvalidTckn_ShouldReturnBadRequestAsync()
    {
        // Arrange
        var command = new ForgotPasswordCommand 
        { 
            TcknOrMemberNumber = "invalid-tckn",
            BirthDate = new DateTime(1990, 1, 1)
        };
        var response = new ForgotPasswordResponse { Message = "Invalid TCKN or member number." };

        _mediator.Send(Arg.Is<ForgotPasswordCommand>(c => c.TcknOrMemberNumber == command.TcknOrMemberNumber), default).Returns(Task.FromResult(response));

        // Act
        var result = await _controller.ForgotPasswordAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<ForgotPasswordResponse>>(okResult.Value);
        apiResponse.Should().NotBeNull();
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Message.Should().Contain("Invalid TCKN or member number.");
    }
}
