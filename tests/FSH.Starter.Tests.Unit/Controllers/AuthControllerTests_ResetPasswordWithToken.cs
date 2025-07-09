using FSH.Starter.WebApi.Host;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;
using MediatR;
using FSH.Framework.Core.Auth.Features.PasswordReset;
using FSH.Starter.WebApi.Contracts.Common;

namespace FSH.Starter.Tests.Unit.Controllers;

public class AuthControllerTests_ResetPasswordWithToken
{
    private readonly IMediator _mediator;
    private readonly AuthController _controller;

    public AuthControllerTests_ResetPasswordWithToken()
    {
        _mediator = Substitute.For<IMediator>();
        _controller = new AuthController(_mediator);
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_SuccessResult_ShouldReturnOkAsync()
    {
        // Arrange
        var command = new ResetPasswordWithTokenCommand 
        { 
            Token = "valid-token", 
            NewPassword = "NewPassword123!" 
        };
        
        _mediator.Send(command, default).Returns(Task.FromResult("Password reset successful"));

        // Act
        var result = await _controller.ResetPasswordWithTokenAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Password reset successful", apiResponse.Data);
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_ErrorResult_ShouldReturnFailureAsync()
    {
        // Arrange
        var command = new ResetPasswordWithTokenCommand 
        { 
            Token = "invalid-token", 
            NewPassword = "NewPassword123!" 
        };
        
        _mediator.Send(command, default).Returns(Task.FromResult("Invalid token or expired"));

        // Act
        var result = await _controller.ResetPasswordWithTokenAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("Invalid token or expired", apiResponse.Message);
    }
}
