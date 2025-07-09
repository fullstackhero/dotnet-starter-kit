using System.Threading.Tasks;
using FSH.Framework.Core.Auth.Features.Admin;
using FSH.Framework.Core.Common.Models;
using FSH.Framework.Server.Controllers;
using FSH.Starter.WebApi.Contracts.Admin;
using FSH.Starter.WebApi.Contracts.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using Xunit;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Dtos;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AdminUsersControllerTests
    {
        private readonly Mock<IMediator> _mediator;
        private readonly Mock<ILogger<AdminUsersController>> _logger;
        private readonly AdminUsersController _controller;

        public AdminUsersControllerTests()
        {
            _mediator = new Mock<IMediator>();
            _logger = new Mock<ILogger<AdminUsersController>>();
            _controller = new AdminUsersController(_mediator.Object, _logger.Object);
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsSuccessAsync()
        {
            // Arrange
            _mediator.Setup(m => m.Send(It.IsAny<GetUsersQuery>(), default))
                .ReturnsAsync(new System.Collections.Generic.List<UserListItemDto>());

            // Act
            var result = await _controller.GetUsersAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CreateUserAsync_InvalidEmail_ReturnsBadRequestAsync()
        {
            // Arrange
            var invalidRequest = new AdminCreateUserCommand
            {
                Email = "invalid",
                Username = "user",
                PhoneNumber = "5555555555",
                Tckn = "12345678901",
                Password = "password",
                FirstName = "Test",
                LastName = "User",
                BirthDate = System.DateTime.Now,
                IsEmailVerified = true
            };

            // Act
            var result = await _controller.CreateUserAsync(invalidRequest);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task CreateUserAsync_InvalidUsername_ReturnsBadRequestAsync()
        {
            // Arrange
            var invalidRequest = new AdminCreateUserCommand
            {
                Email = "test@example.com",
                Username = "!invalid-username",
                PhoneNumber = "5555555555",
                Tckn = "12345678901",
                Password = "password",
                FirstName = "Test",
                LastName = "User",
                BirthDate = System.DateTime.Now,
                IsEmailVerified = true
            };

            // Act
            var result = await _controller.CreateUserAsync(invalidRequest);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task CreateUserAsync_InvalidPhone_ReturnsBadRequestAsync()
        {
            // Arrange
            var invalidRequest = new AdminCreateUserCommand
            {
                Email = "test@example.com",
                Username = "validuser",
                PhoneNumber = "invalidphone",
                Tckn = "12345678901",
                Password = "password",
                FirstName = "Test",
                LastName = "User",
                BirthDate = System.DateTime.Now,
                IsEmailVerified = true
            };

            // Act
            var result = await _controller.CreateUserAsync(invalidRequest);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task CreateUserAsync_InvalidTckn_ReturnsBadRequestAsync()
        {
            // Arrange
            var invalidRequest = new AdminCreateUserCommand
            {
                Email = "test@example.com",
                Username = "validuser",
                PhoneNumber = "5555555555",
                Tckn = "invalidtckn",
                Password = "password",
                FirstName = "Test",
                LastName = "User",
                BirthDate = System.DateTime.Now,
                IsEmailVerified = true
            };

            // Act
            var result = await _controller.CreateUserAsync(invalidRequest);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task CreateUserAsync_InvalidPassword_ReturnsBadRequestAsync()
        {
            // Arrange
            var invalidRequest = new AdminCreateUserCommand
            {
                Email = "test@example.com",
                Username = "validuser",
                PhoneNumber = "5555555555",
                Tckn = "12345678901",
                Password = "short",
                FirstName = "Test",
                LastName = "User",
                BirthDate = System.DateTime.Now,
                IsEmailVerified = true
            };

            // Act
            var result = await _controller.CreateUserAsync(invalidRequest);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task CreateUserAsync_ValidRequest_ReturnsCreatedAsync()
        {
            // Arrange
            var command = new AdminCreateUserCommand
            {
                Email = "test@example.com",
                Username = "validuser",
                PhoneNumber = "5555555555",
                Tckn = "10000000146",
                Password = "ValidPassword123!",
                FirstName = "Test",
                LastName = "User",
                BirthDate = System.DateTime.Now.AddYears(-25).Date,
                IsEmailVerified = true,
                ProfessionId = 1,
                Status = "ACTIVE"
            };

            _mediator.Setup(m => m.Send(It.IsAny<CreateUserCommand>(), default))
                .ReturnsAsync(Result<CreateUserResult>.Success(new CreateUserResult
                {
                    UserId = Guid.NewGuid(),
                    Email = "test@example.com",
                    Username = "validuser",
                    MemberNumber = "12345",
                    Message = "User created"
                }));

            // Act
            var result = await _controller.CreateUserAsync(command);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var response = Assert.IsType<ApiResponse<CreateUserResult>>(createdResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
        }
    }
}
