using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Auth.Features.Admin;
using FSH.Framework.Core.Auth.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FSH.Starter.Tests.Unit.Helpers;

namespace FSH.Starter.Tests.Unit.Features.Admin
{
    public class AssignRoleCommandHandlerTests
    {
        [Fact]
        public Task Handle_UserNotFound_ThrowsKeyNotFoundExceptionAsync()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AppUser)null!);
            var logger = new Mock<ILogger<AssignRoleCommandHandler>>();
            var handler = new AssignRoleCommandHandler(userRepo.Object, logger.Object);
            var command = new AssignRoleCommand { UserId = Guid.NewGuid(), Role = "user" };

            return Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_RoleAlreadyAssigned_ReturnsAlreadyAssignedMessageAsync()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(AppUserFactory.Create());
            userRepo.Setup(r => r.GetUserRolesAsync(It.IsAny<Guid>())).ReturnsAsync(new List<string> { "user" });
            var logger = new Mock<ILogger<AssignRoleCommandHandler>>();
            var handler = new AssignRoleCommandHandler(userRepo.Object, logger.Object);
            var command = new AssignRoleCommand { UserId = Guid.NewGuid(), Role = "user" };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal("Role was already assigned to the user", result.Message);
        }

        [Fact]
        public async Task Handle_AssignsRoleSuccessfully_ReturnsSuccessMessageAsync()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(AppUserFactory.Create());
            userRepo.Setup(r => r.GetUserRolesAsync(It.IsAny<Guid>())).ReturnsAsync(new List<string>());
            userRepo.Setup(r => r.AssignRoleAsync(It.IsAny<Guid>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            var logger = new Mock<ILogger<AssignRoleCommandHandler>>();
            var handler = new AssignRoleCommandHandler(userRepo.Object, logger.Object);
            var command = new AssignRoleCommand { UserId = Guid.NewGuid(), Role = "user" };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal("Role assigned successfully", result.Message);
        }
    }
}
