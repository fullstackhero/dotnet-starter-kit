using System;
using System.Threading.Tasks;
using FSH.Framework.Core.Auth.Features.PasswordReset;
using FSH.Starter.WebApi.Contracts.Common;
using FSH.Starter.WebApi.Host;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests_ResetPassword
    {
        private readonly Mock<IMediator> _mediator;
        private readonly AuthController _controller;

        public AuthControllerTests_ResetPassword()
        {
            _mediator = new Mock<IMediator>();
            _controller = new AuthController(_mediator.Object);
        }

        [Fact]
        public async Task ResetPasswordWithTokenAsync_ValidToken_ReturnsOkAsync()
        {
            // Arrange
            var request = new ResetPasswordWithTokenCommand { NewPassword = "newPassword123!", Token = "validToken" };
            _mediator.Setup(m => m.Send(It.IsAny<ResetPasswordWithTokenCommand>(), default))
                .ReturnsAsync("Password reset successful.");

            // Act
            var result = await _controller.ResetPasswordWithTokenAsync(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("Operation successful", apiResponse.Message);
            Assert.Equal("Password reset successful.", apiResponse.Data);
        }

        [Fact]
        public async Task ResetPasswordWithTokenAsync_MediatorReturnsError_ReturnsOkWithFailureAsync()
        {
            // Arrange
            var request = new ResetPasswordWithTokenCommand { NewPassword = "newPassword123!", Token = "invalidToken" };
            _mediator.Setup(m => m.Send(It.IsAny<ResetPasswordWithTokenCommand>(), default))
                .ReturnsAsync("Invalid token.");

            // Act
            var result = await _controller.ResetPasswordWithTokenAsync(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("invalid token", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
