using System.Threading.Tasks;
using FSH.Framework.Server.Controllers;
using FSH.Starter.WebApi.Contracts.Admin;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FSH.Framework.Core.Auth.Features.Admin;
using FSH.Framework.Core.Common.Models;
using FSH.Framework.Core.Auth.Dtos;
using Microsoft.Extensions.Logging;
using AdminAuth = FSH.Framework.Core.Auth.Features.Admin;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AdminUsersControllerTests_Enterprise
    {
        private readonly Mock<IMediator> _mediator;
        private readonly Mock<ILogger<AdminUsersController>> _logger;
        private readonly AdminUsersController _controller;

        public AdminUsersControllerTests_Enterprise()
        {
            _mediator = new Mock<IMediator>();
            _logger = new Mock<ILogger<AdminUsersController>>();
            _controller = new AdminUsersController(_mediator.Object, _logger.Object);
        }

        [Fact]
        public async Task UpdateUserAsync_ValidRequest_ReturnsSuccessAsync()
        {
            // Arrange
            _mediator.Setup(m => m.Send(It.IsAny<UpdateUserCommand>(), default))
                .ReturnsAsync(Result<UpdateUserResult>.Success(new UpdateUserResult { }));
            var id = System.Guid.NewGuid();
            var request = new AdminUpdateUserCommand
            {
                Email = "test@example.com",
                Username = "testuser",
                FirstName = "Test",
                LastName = "User",
                PhoneNumber = "5555555555",
                ProfessionId = 1,
                Status = "ACTIVE",
                IsEmailVerified = true
            };

            // Act
            var result = await _controller.UpdateUserAsync(id, request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUserAsync_InvalidRequest_ReturnsBadRequestAsync()
        {
            // Arrange
            var id = System.Guid.NewGuid();
            var request = new AdminUpdateUserCommand
            {
                Email = "", // invalid
                Username = "",
                FirstName = "",
                LastName = "",
                PhoneNumber = "",
                ProfessionId = null,
                Status = "",
                IsEmailVerified = false
            };

            // Act
            var result = await _controller.UpdateUserAsync(id, request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteUserAsync_ValidId_ReturnsSuccessAsync()
        {
            // Arrange
            _mediator.Setup(m => m.Send(It.IsAny<AdminAuth.DeleteUserCommand>(), default))
                .ReturnsAsync(new DeleteUserResult());
            var id = System.Guid.NewGuid();

            // Act
            var result = await _controller.DeleteUserAsync(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task AssignRoleAsync_ValidRequest_ReturnsSuccessAsync()
        {
            // Arrange
            _mediator.Setup(m => m.Send(It.IsAny<AdminAuth.AssignRoleCommand>(), default))
                .ReturnsAsync(new AssignRoleResult());
            var id = System.Guid.NewGuid();
            var request = new AdminAssignRoleRequest { Role = "admin" };

            // Act
            var result = await _controller.AssignRoleAsync(id, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
