using System;
using System.Linq;
using System.Threading.Tasks;
using FSH.Framework.Core.Auth.Features.Login;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using FSH.Framework.Core.Auth.Dtos;
using FSH.Framework.Core.Common.Models;
using FSH.Starter.WebApi.Contracts.Auth;
using FSH.Starter.WebApi.Contracts.Common;
using FSH.Starter.WebApi.Host;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests_Login
    {
        private readonly Mock<IMediator> _mediator;
        private readonly AuthController _controller;

        public AuthControllerTests_Login()
        {
            _mediator = new Mock<IMediator>();
            _controller = new AuthController(_mediator.Object);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsOkWithTokenAsync()
        {
            // Arrange
            var loginRequest = new LoginRequest { TcknOrMemberNumber = "12345678901", Password = "Password123!" };
            var loginResponseDto = new LoginResponseDto { AccessToken = "some.token", RefreshToken = "some.refresh.token", UserId = Guid.NewGuid(), Email = "test@test.com", Username = "testuser" };
            var successResult = Result<LoginResponseDto>.Success(loginResponseDto);

            _mediator.Setup(m => m.Send(It.IsAny<LoginCommand>(), default))
                .ReturnsAsync(successResult);

            // Act
            var result = await _controller.LoginAsync(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<LoginResponseDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(loginResponseDto, apiResponse.Data);
        }

        [Fact]
        public async Task LoginAsync_MediatorReturnsFailure_ReturnsOkWithFailureAsync()
        {
            // Arrange
            var loginRequest = new LoginRequest { TcknOrMemberNumber = "12345678901", Password = "Password123!" };
            var failureResult = Result<LoginResponseDto>.Failure("Invalid credentials");

            _mediator.Setup(m => m.Send(It.IsAny<LoginCommand>(), default))
                .ReturnsAsync(failureResult);

            // Act
            var result = await _controller.LoginAsync(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<LoginResponseDto>>(okResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Equal("Invalid credentials", apiResponse.Message);
        }

        [Fact]
        public async Task LoginAsync_InvalidPasswordFormat_ReturnsBadRequestAsync()
        {
            // Arrange
            var request = new LoginRequest { TcknOrMemberNumber = "12345678901", Password = "short" };

            // Act
            var result = await _controller.LoginAsync(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Password must be at least 8 characters", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LoginAsync_EmptyPassword_ReturnsBadRequestAsync()
        {
            // Arrange
            var loginRequest = new LoginRequest { TcknOrMemberNumber = "12345678901", Password = "" };

            // Act
            var result = await _controller.LoginAsync(loginRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            // Debug what we're actually getting
            var actualMessage = apiResponse.Message ?? "";
            var actualErrors = apiResponse.Errors?.FirstOrDefault() ?? "";
            Assert.True(
                actualMessage.Contains("Password cannot be empty", StringComparison.OrdinalIgnoreCase) ||
                actualErrors.Contains("Password cannot be empty", StringComparison.OrdinalIgnoreCase) ||
                actualMessage.Contains("şifre", StringComparison.OrdinalIgnoreCase), 
                $"Expected password empty error but got Message: '{actualMessage}', First Error: '{actualErrors}'"
            );
        }

        [Fact]
        public async Task LoginAsync_ShortPassword_ReturnsBadRequestAsync()
        {
            // Arrange
            var loginRequest = new LoginRequest { TcknOrMemberNumber = "12345678901", Password = "short" };

            // Act
            var result = await _controller.LoginAsync(loginRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            // Debug what we're actually getting
            var actualMessage = apiResponse.Message ?? "";
            var actualErrors = apiResponse.Errors?.FirstOrDefault() ?? "";
            Assert.True(
                actualMessage.Contains("Password must be at least 8 characters", StringComparison.OrdinalIgnoreCase) ||
                actualErrors.Contains("Password must be at least 8 characters", StringComparison.OrdinalIgnoreCase) ||
                actualMessage.Contains("8", StringComparison.OrdinalIgnoreCase), 
                $"Expected password length error but got Message: '{actualMessage}', First Error: '{actualErrors}'"
            );
        }

        [Fact]
        public async Task LoginAsync_PasswordWithoutUppercase_ReturnsBadRequestAsync()
        {
            // Arrange
            var loginRequest = new LoginRequest { TcknOrMemberNumber = "12345678901", Password = "password123!" };

            // Act
            var result = await _controller.LoginAsync(loginRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            // Debug what we're actually getting
            var actualMessage = apiResponse.Message ?? "";
            var actualErrors = apiResponse.Errors?.FirstOrDefault() ?? "";
            Assert.True(
                actualMessage.Contains("Password must contain at least one uppercase letter", StringComparison.OrdinalIgnoreCase) ||
                actualErrors.Contains("Password must contain at least one uppercase letter", StringComparison.OrdinalIgnoreCase) ||
                actualMessage.Contains("uppercase", StringComparison.OrdinalIgnoreCase), 
                $"Expected uppercase error but got Message: '{actualMessage}', First Error: '{actualErrors}'"
            );
        }

        [Fact]
        public async Task LoginAsync_ValidPasswordFormat_ShouldCallMediatorAsync()
        {
            // Arrange
            var loginRequest = new LoginRequest { TcknOrMemberNumber = "12345678901", Password = "ValidPassword123!" };
            var successResult = Result<LoginResponseDto>.Success(new LoginResponseDto 
            { 
                AccessToken = "token", 
                RefreshToken = "refresh", 
                UserId = Guid.NewGuid(),
                Email = "test@test.com",
                Username = "testuser"
            });

            _mediator.Setup(m => m.Send(It.IsAny<LoginCommand>(), default))
                .ReturnsAsync(successResult);

            // Act
            var result = await _controller.LoginAsync(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<LoginResponseDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            
            // Verify that mediator was called with proper command
            _mediator.Verify(m => m.Send(It.Is<LoginCommand>(c => 
                c.TcknOrMemberNumber == loginRequest.TcknOrMemberNumber), default), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_MediatorReturnsError_ShouldReturnFailureAsync()
        {
            // Arrange
            var loginRequest = new LoginRequest { TcknOrMemberNumber = "12345678901", Password = "ValidPassword123!" };
            var failureResult = Result<LoginResponseDto>.Failure("Login failed");

            _mediator.Setup(m => m.Send(It.IsAny<LoginCommand>(), default))
                .ReturnsAsync(failureResult);

            // Act
            var result = await _controller.LoginAsync(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<LoginResponseDto>>(okResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Equal("Login failed", apiResponse.Message);
        }
    }
}
