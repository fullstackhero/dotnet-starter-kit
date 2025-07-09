using System.Threading.Tasks;
using FSH.Starter.WebApi.Host;
using FSH.Framework.Core.Auth.Features.PasswordReset;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests_ResetMethods
    {
        [Fact]
        public async Task SelectResetMethodAsync_ValidRequest_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<SelectResetMethodCommand>(), default))
                .ReturnsAsync("Email");
            var controller = new AuthController(mediator.Object);
            var command = new SelectResetMethodCommand();

            var result = await controller.SelectResetMethodAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
