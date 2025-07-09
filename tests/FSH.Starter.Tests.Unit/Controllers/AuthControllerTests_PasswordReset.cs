using System.Threading.Tasks;
using FSH.Starter.WebApi.Host;
using FSH.Framework.Core.Auth.Features.PasswordReset;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests_PasswordReset
    {
        [Fact]
        public async Task ForgotPasswordAsync_ValidRequest_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<ForgotPasswordCommand>(), default))
                .ReturnsAsync(new ForgotPasswordResponse());
            var controller = new AuthController(mediator.Object);
            var command = new ForgotPasswordCommand();

            var result = await controller.ForgotPasswordAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidRequest_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<ResetPasswordCommand>(), default))
                .ReturnsAsync("Success");
            var controller = new AuthController(mediator.Object);
            var command = new ResetPasswordCommand();

            var result = await controller.ResetPasswordAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidRequest_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<ChangePasswordCommand>(), default))
                .ReturnsAsync("Success");
            var controller = new AuthController(mediator.Object);
            var command = new ChangePasswordCommand();

            var result = await controller.ChangePasswordAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
