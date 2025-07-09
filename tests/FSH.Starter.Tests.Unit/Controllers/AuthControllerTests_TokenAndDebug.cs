using System.Threading.Tasks;
using FSH.Starter.WebApi.Host;
using FSH.Framework.Core.Auth.Features.Token.Generate;
using FSH.Framework.Core.Auth.Features.Token.Refresh;
using FSH.Framework.Core.Auth.Features.Identity;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests_TokenAndDebug
    {
        [Fact]
        public async Task TestMernisAsync_ValidRequest_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<TestMernisRequest>(), default))
                .ReturnsAsync(new TestMernisResult());
            var controller = new AuthController(mediator.Object);
            var request = new TestMernisRequest();

            var result = await controller.TestMernisAsync(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetDebugTokens_ReturnsOk()
        {
            var mediator = new Mock<IMediator>();
            var controller = new AuthController(mediator.Object);
            var result = controller.GetDebugTokens();
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
