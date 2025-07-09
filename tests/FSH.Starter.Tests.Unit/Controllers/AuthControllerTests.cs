using System.Threading.Tasks;
using FSH.Starter.WebApi.Host;
using FSH.Starter.WebApi.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests
    {
        [Fact]
        public void Test_ReturnsOk()
        {
            // Arrange
            var mediator = new Mock<IMediator>();
            var controller = new AuthController(mediator.Object);

            // Act
            var result = controller.Test();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsBadRequestAsync()
        {
            // Arrange
            var mediator = new Mock<IMediator>();
            var controller = new AuthController(mediator.Object);
            var request = new LoginRequest { TcknOrMemberNumber = "12345678901", Password = "" };

            // Act
            var result = await controller.LoginAsync(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }
    }
}
