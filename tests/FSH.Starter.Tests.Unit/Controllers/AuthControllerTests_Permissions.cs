using System.Collections.Generic;
using System.Security.Claims;
using FSH.Starter.WebApi.Host;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests_Permissions
    {
        private readonly Mock<IMediator> _mediator;
        private readonly AuthController _controller;

        public AuthControllerTests_Permissions()
        {
            _mediator = new Mock<IMediator>();
            _controller = new AuthController(_mediator.Object);
        }

        [Fact]
        public void GetPermissions_UserIsAuthenticated_ReturnsPermissions()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = _controller.GetPermissions();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var permissions = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(2, permissions.Count);
            Assert.Contains("Admin", permissions);
            Assert.Contains("User", permissions);
        }

        [Fact]
        public void GetPermissions_UserIsAuthenticated_ButHasNoRoles_ReturnsEmptyList()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Test User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = _controller.GetPermissions();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var permissions = Assert.IsType<List<string>>(okResult.Value);
            Assert.Empty(permissions);
        }
    }
}
