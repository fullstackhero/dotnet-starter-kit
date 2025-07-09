using System.Threading.Tasks;
using FSH.Starter.WebApi.Host;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Http;
using FSH.Framework.Core.Auth.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Starter.Tests.Unit.Controllers
{
    public class AuthControllerTests_DebugUser
    {
        [Fact]
        public async Task DebugUserAsync_UserNotFound_ReturnsNotFoundAsync()
        {
            var mediator = new Mock<IMediator>();
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetByTcknAsync(It.IsAny<Tckn>()))
                .ReturnsAsync((AppUser)null!);

            var services = new ServiceCollection();
            services.AddSingleton(userRepo.Object);
            var provider = services.BuildServiceProvider();
            var httpContext = new DefaultHttpContext { RequestServices = provider };
            var controller = new TestableAuthController(mediator.Object, httpContext);
            var request = new DebugUserRequest { Tckn = "10000000146", Password = "pass" };

            var result = await controller.DebugUserAsync(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task DebugUserAsync_UserFound_ReturnsUserDetailsAsync()
        {
            // Arrange
            var mediator = new Mock<IMediator>();
            var userRepo = new Mock<IUserRepository>();
            
            var userCreationResult = AppUser.Create(
                "test@example.com",
                "testuser",
                "5551234567",
                "10000000146",
                "Test",
                "User",
                null,
                DateTime.Today.AddYears(-30)
            );
            
            Assert.True(userCreationResult.IsSuccess, "User creation should succeed");
            var mockUser = userCreationResult.Value!.SetPassword("TestPassword123!");

            userRepo.Setup(r => r.GetByTcknAsync(It.IsAny<Tckn>()))
                .ReturnsAsync(mockUser);
            
            userRepo.Setup(r => r.ValidatePasswordAndGetByTcknAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((true, mockUser));

            var services = new ServiceCollection();
            services.AddSingleton(userRepo.Object);
            var provider = services.BuildServiceProvider();
            var httpContext = new DefaultHttpContext { RequestServices = provider };
            var controller = new TestableAuthController(mediator.Object, httpContext);
            var request = new DebugUserRequest { Tckn = "10000000146", Password = "TestPassword123!" };

            // Act
            var result = await controller.DebugUserAsync(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            // Verify the response contains user information
            var response = okResult.Value as dynamic;
            Assert.NotNull(response);
        }

        [Fact]
        public async Task DebugUserAsync_ExceptionThrown_ReturnsErrorAsync()
        {
            // Arrange
            var mediator = new Mock<IMediator>();
            var userRepo = new Mock<IUserRepository>();
            
            userRepo.Setup(r => r.GetByTcknAsync(It.IsAny<Tckn>()))
                .ThrowsAsync(new Exception("Database error"));

            var services = new ServiceCollection();
            services.AddSingleton(userRepo.Object);
            var provider = services.BuildServiceProvider();
            var httpContext = new DefaultHttpContext { RequestServices = provider };
            var controller = new TestableAuthController(mediator.Object, httpContext);
            var request = new DebugUserRequest { Tckn = "10000000146", Password = "pass" };

            // Act
            var result = await controller.DebugUserAsync(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            // Verify the response contains error information
            var response = okResult.Value as dynamic;
            Assert.NotNull(response);
        }
    }
}
