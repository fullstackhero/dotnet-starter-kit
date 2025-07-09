using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FSH.Framework.Core.Auth.Domain;
using FSH.Framework.Core.Auth.Features.Admin;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Starter.Tests.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FSH.Starter.Tests.Unit.Features.Admin
{
    public class DeleteUserCommandHandlerTests
    {
        [Fact]
        public Task Handle_UserNotFound_ThrowsKeyNotFoundExceptionAsync()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AppUser)null!);
            var logger = new Mock<ILogger<DeleteUserCommandHandler>>();
            var handler = new DeleteUserCommandHandler(userRepo.Object, logger.Object);
            var command = new DeleteUserCommand { UserId = Guid.NewGuid() };

            return Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public Task Handle_UserIsAdmin_ThrowsInvalidOperationExceptionAsync()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(AppUserFactory.Create());
            userRepo.Setup(r => r.GetUserRolesAsync(It.IsAny<Guid>())).ReturnsAsync(new List<string> { "admin" });
            var logger = new Mock<ILogger<DeleteUserCommandHandler>>();
            var handler = new DeleteUserCommandHandler(userRepo.Object, logger.Object);
            var command = new DeleteUserCommand { UserId = Guid.NewGuid() };

            return Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        }
    }
}
