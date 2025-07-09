using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FSH.Starter.WebApi.Host;
using FSH.Framework.Core.Auth.Features.Profile;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests_ProfileUpdate
    {
        private static AuthController CreateControllerWithUser(IMediator mediator, string userId)
        {
            var httpContext = new DefaultHttpContext();
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var controller = new TestableAuthController(mediator, httpContext);
            return controller;
        }

        [Fact]
        public async Task UpdateEmailAsync_ValidUser_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<UpdateEmailCommand>(), default))
                .ReturnsAsync("Success");
            var controller = CreateControllerWithUser(mediator.Object, Guid.NewGuid().ToString());
            var command = new UpdateEmailCommand();

            var result = await controller.UpdateEmailAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var value = okResult.Value;
        }

        [Fact]
        public async Task UpdatePhoneAsync_ValidUser_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<UpdatePhoneCommand>(), default))
                .ReturnsAsync("Success");
            var controller = CreateControllerWithUser(mediator.Object, Guid.NewGuid().ToString());
            var command = new UpdatePhoneCommand();

            var result = await controller.UpdatePhoneAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var value = okResult.Value;
        }

        [Fact]
        public async Task VerifyEmailUpdateAsync_ValidUser_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<VerifyEmailUpdateCommand>(), default))
                .ReturnsAsync("Success");
            var controller = CreateControllerWithUser(mediator.Object, Guid.NewGuid().ToString());
            var command = new VerifyEmailUpdateCommand();

            var result = await controller.VerifyEmailUpdateAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var value = okResult.Value;
        }

        [Fact]
        public async Task VerifyPhoneUpdateAsync_ValidUser_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<VerifyPhoneUpdateCommand>(), default))
                .ReturnsAsync("Success");
            var controller = CreateControllerWithUser(mediator.Object, Guid.NewGuid().ToString());
            var command = new VerifyPhoneUpdateCommand();

            var result = await controller.VerifyPhoneUpdateAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var value = okResult.Value;
        }
    }
}
