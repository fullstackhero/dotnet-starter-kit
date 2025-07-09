using FSH.Starter.WebApi.Host;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Xunit;
using MediatR;
using FSH.Framework.Core.Auth.Features.Token.Generate;
using FluentAssertions;
using FSH.Starter.WebApi.Contracts.Common;

namespace FSH.Starter.Tests.Unit.Controllers;

public class AuthControllerTests_Token
{
    private readonly IMediator _mediator;
    private readonly AuthController _controller;

    public AuthControllerTests_Token()
    {
        _mediator = Substitute.For<IMediator>();
        _controller = new AuthController(_mediator);
    }

    [Fact]
    public async Task GenerateTokenAsync_ValidRequest_ShouldReturnOkAsync()
    {
        // Arrange
        var command = new GenerateTokenCommand
        {
            Tckn = "12345678901",
            Password = "TestPassword123!"
        };
        var response = new TokenGenerationResult
        {
            Token = "generated-token-123",
            RefreshToken = "refresh-token-456",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30)
        };
        _mediator.Send(command, default).Returns(response);

        // Act
        var result = await _controller.GenerateTokenAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<TokenGenerationResult>>(okResult.Value);
        apiResponse.Should().NotBeNull();
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().Be(response);
    }
}
