using System.Threading;
using System.Threading.Tasks;
using FSH.Starter.WebApi.Host;
using FSH.Framework.Core.Auth.Features.PasswordReset;
using FSH.Framework.Core.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests_Validation
    {
        [Fact]
        public async Task ValidateResetTokenAsync_ValidRequest_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<ValidateResetTokenCommand>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result<ValidateResetTokenResponse>.Success(new ValidateResetTokenResponse())));
            var controller = new AuthController(mediator.Object);
            var command = new ValidateResetTokenCommand();

            var result = await controller.ValidateResetTokenAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ValidateTcPhoneAsync_ValidRequest_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<ValidateTcPhoneCommand>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult("Success"));
            var controller = new AuthController(mediator.Object);
            var command = new ValidateTcPhoneCommand();

            var result = await controller.ValidateTcPhoneAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
