using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FSH.Framework.Core.Auth.Features.PasswordReset;
using FSH.Framework.Core.Auth.Features.Profile;
using FSH.Framework.Core.Auth.Features.Token.Generate;
using FSH.Framework.Core.Auth.Features.Token.Refresh;
using FSH.Framework.Core.Auth.Features.VerifyRegistration;
using FSH.Framework.Core.Auth.Dtos;
using FSH.Framework.Core.Common.Models;
using FSH.Starter.WebApi.Contracts.Auth;
using FSH.Starter.WebApi.Contracts.Common;
using FSH.Starter.WebApi.Host;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using System.Security.Claims;

namespace FSH.Starter.Tests.Unit.Controllers;

public class AuthControllerTests_AdditionalCoverage
{
    private readonly Mock<IMediator> _mediator;
    private readonly AuthController _controller;

    public AuthControllerTests_AdditionalCoverage()
    {
        _mediator = new Mock<IMediator>();
        _controller = new AuthController(_mediator.Object);
        
        // Setup HttpContext for tests that need it
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext,
        };
    }

    [Fact]
    public async Task SelectResetMethodAsync_ValidRequest_ReturnsOkAsync()
    {
        // Arrange
        var command = new SelectResetMethodCommand();
        _mediator.Setup(m => m.Send(It.IsAny<SelectResetMethodCommand>(), default))
            .ReturnsAsync("Reset method selected");

        // Act
        var result = await _controller.SelectResetMethodAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Reset method selected", apiResponse.Data);
    }

    [Fact]
    public async Task ValidateTcPhoneAsync_ValidRequest_ReturnsOkAsync()
    {
        // Arrange
        var command = new FSH.Framework.Core.Auth.Features.PasswordReset.ValidateTcPhoneCommand();
        _mediator.Setup(m => m.Send(It.IsAny<FSH.Framework.Core.Auth.Features.PasswordReset.ValidateTcPhoneCommand>(), default))
            .ReturnsAsync("Validation successful");

        // Act
        var result = await _controller.ValidateTcPhoneAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Validation successful", apiResponse.Data);
    }

    [Fact]
    public async Task ValidateResetTokenAsync_ValidRequest_ReturnsOkAsync()
    {
        // Arrange
        var command = new ValidateResetTokenCommand();
        var response = new ValidateResetTokenResponse();
        _mediator.Setup(m => m.Send(It.IsAny<ValidateResetTokenCommand>(), default))
            .ReturnsAsync(Result<ValidateResetTokenResponse>.Success(response));

        // Act
        var result = await _controller.ValidateResetTokenAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<ValidateResetTokenResponse>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(response, apiResponse.Data);
    }

    [Fact]
    public async Task GenerateTokenAsync_ValidRequest_ReturnsOkAsync()
    {
        // Arrange
        var command = new GenerateTokenCommand();
        var tokenResult = new TokenGenerationResult();
        _mediator.Setup(m => m.Send(It.IsAny<GenerateTokenCommand>(), default))
            .ReturnsAsync(tokenResult);

        // Act
        var result = await _controller.GenerateTokenAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<TokenGenerationResult>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(tokenResult, apiResponse.Data);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidRequest_ReturnsOkAsync()
    {
        // Arrange
        var command = new RefreshTokenCommand();
        var tokenResponse = new TokenResponseDto();
        _mediator.Setup(m => m.Send(It.IsAny<RefreshTokenCommand>(), default))
            .ReturnsAsync(tokenResponse);

        // Act
        var result = await _controller.RefreshTokenAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<TokenResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(tokenResponse, apiResponse.Data);
    }

    [Fact]
    public void GetPermissions_WithUserRoles_ReturnsRolesList()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "user")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext.HttpContext.User = principal;

        // Act
        var result = _controller.GetPermissions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var roles = Assert.IsType<List<string>>(okResult.Value);
        Assert.Contains("admin", roles);
        Assert.Contains("user", roles);
    }

    [Fact]
    public void GetPermissions_WithNoRoles_ReturnsEmptyList()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = principal;

        // Act
        var result = _controller.GetPermissions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var roles = Assert.IsType<List<string>>(okResult.Value);
        Assert.Empty(roles);
    }

    [Fact]
    public async Task GetProfileAsync_ValidUser_ReturnsOkAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userDetail = new UserDetailDto
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser"
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        _mediator.Setup(m => m.Send(It.IsAny<GetUserProfileQuery>(), default))
            .ReturnsAsync(userDetail);

        // Act
        var result = await _controller.GetProfileAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<UserDetailDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(userDetail, apiResponse.Data);
    }

    [Fact]
    public async Task GetProfileAsync_ExceptionThrown_ReturnsErrorAsync()
    {
        // Arrange - No user claims set, so it should return failure
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetProfileAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<UserDetailDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Unable to determine current user", apiResponse.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateProfileAsync_ValidUser_ReturnsOkAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateProfileCommand();
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
        
        _mediator.Setup(m => m.Send(It.IsAny<UpdateProfileCommand>(), default))
            .ReturnsAsync("Profile updated successfully");

        // Act
        var result = await _controller.UpdateProfileAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Profile updated successfully", apiResponse.Data);
    }

    [Fact]
    public async Task UpdateEmailAsync_ValidUser_ReturnsOkAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateEmailCommand();
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
        
        _mediator.Setup(m => m.Send(It.IsAny<UpdateEmailCommand>(), default))
            .ReturnsAsync("Email update initiated");

        // Act
        var result = await _controller.UpdateEmailAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Email update initiated", apiResponse.Data);
    }

    [Fact]
    public async Task UpdatePhoneAsync_ValidUser_ReturnsOkAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdatePhoneCommand();
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
        
        _mediator.Setup(m => m.Send(It.IsAny<UpdatePhoneCommand>(), default))
            .ReturnsAsync("Phone update initiated");

        // Act
        var result = await _controller.UpdatePhoneAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Phone update initiated", apiResponse.Data);
    }

    [Fact]
    public async Task VerifyEmailUpdateAsync_ValidUser_ReturnsOkAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new VerifyEmailUpdateCommand();
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
        
        _mediator.Setup(m => m.Send(It.IsAny<VerifyEmailUpdateCommand>(), default))
            .ReturnsAsync("Email verified successfully");

        // Act
        var result = await _controller.VerifyEmailUpdateAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Email verified successfully", apiResponse.Data);
    }

    [Fact]
    public async Task VerifyPhoneUpdateAsync_ValidUser_ReturnsOkAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new VerifyPhoneUpdateCommand();
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
        
        _mediator.Setup(m => m.Send(It.IsAny<VerifyPhoneUpdateCommand>(), default))
            .ReturnsAsync("Phone verified successfully");

        // Act
        var result = await _controller.VerifyPhoneUpdateAsync(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Phone verified successfully", apiResponse.Data);
    }
}
