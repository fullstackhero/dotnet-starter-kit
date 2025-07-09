using FSH.Starter.WebApi.Host;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Xunit;
using MediatR;
using FSH.Framework.Core.Auth.Features.Token.Refresh;
using FluentAssertions;
using FSH.Starter.WebApi.Contracts.Common;

namespace FSH.Starter.Tests.Unit.Controllers;

public class AuthControllerTests_RefreshToken
{
    private readonly IMediator _mediator;
    private readonly AuthController _controller;

    public AuthControllerTests_RefreshToken()
    {
        _mediator = Substitute.For<IMediator>();
        _controller = new AuthController(_mediator);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidRequest_ShouldReturnOkAsync()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            Token = "current-access-token",
            RefreshToken = "valid-refresh-token"
        };
        var response = new FSH.Framework.Core.Auth.Dtos.TokenResponseDto
        {
            Token = "new-access-token",
            RefreshToken = "new-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30)
        };
        _mediator.Send(command, default).Returns(response);

        // Act
        var result = await _controller.RefreshTokenAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<FSH.Framework.Core.Auth.Dtos.TokenResponseDto>>(okResult.Value);
        apiResponse.Should().NotBeNull();
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().Be(response);
    }
}
