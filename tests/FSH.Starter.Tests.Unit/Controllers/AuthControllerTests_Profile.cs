using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FSH.Starter.WebApi.Host;
using FSH.Framework.Core.Auth.Features.Profile;
using FSH.Framework.Core.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests_Profile
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
        public async Task GetProfileAsync_ValidUser_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<GetUserProfileQuery>(), default))
                .ReturnsAsync(new UserDetailDto());
            var controller = CreateControllerWithUser(mediator.Object, Guid.NewGuid().ToString());

            var result = await controller.GetProfileAsync();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetProfileAsync_InvalidUser_ReturnsFailureAsync()
        {
            var mediator = new Mock<IMediator>();
            var controller = CreateControllerWithUser(mediator.Object, "");

            var result = await controller.GetProfileAsync();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateProfileAsync_ValidUser_ReturnsOkAsync()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<UpdateProfileCommand>(), default))
                .ReturnsAsync("Success");
            var controller = CreateControllerWithUser(mediator.Object, Guid.NewGuid().ToString());
            var command = new UpdateProfileCommand();

            var result = await controller.UpdateProfileAsync(command);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
