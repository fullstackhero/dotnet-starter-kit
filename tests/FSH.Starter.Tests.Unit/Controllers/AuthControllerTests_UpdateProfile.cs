using FSH.Starter.WebApi.Host;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Xunit;
using MediatR;
using FSH.Framework.Core.Auth.Features.Profile;
using FluentAssertions;
using FSH.Starter.WebApi.Contracts.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace FSH.Starter.Tests.Unit.Controllers;

public class AuthControllerTests_UpdateProfile
{
    private readonly IMediator _mediator;
    private readonly AuthController _controller;

    public AuthControllerTests_UpdateProfile()
    {
        _mediator = Substitute.For<IMediator>();
        _controller = new AuthController(_mediator);

        // Setup HTTP context with claims
        var httpContext = new DefaultHttpContext();
        var userId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new System.Security.Claims.Claim("uid", userId),
            new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task UpdateEmailAsync_ValidRequest_ShouldReturnOkAsync()
    {
        // Arrange
        var command = new UpdateEmailCommand
        {
            NewEmail = "newemail@test.com"
        };
        var response = "Email update initiated successfully";
        _mediator.Send(Arg.Any<UpdateEmailCommand>(), default).Returns(Task.FromResult(response));

        // Act
        var result = await _controller.UpdateEmailAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        apiResponse.Should().NotBeNull();
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task UpdatePhoneAsync_ValidRequest_ShouldReturnOkAsync()
    {
        // Arrange
        var command = new UpdatePhoneCommand
        {
            NewPhoneNumber = "05551234567"
        };
        var response = "Phone update initiated successfully";
        _mediator.Send(Arg.Any<UpdatePhoneCommand>(), default).Returns(Task.FromResult(response));

        // Act
        var result = await _controller.UpdatePhoneAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        apiResponse.Should().NotBeNull();
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().Be(response);
    }
}
