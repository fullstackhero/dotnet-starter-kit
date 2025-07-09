using System;
using System.Threading;
using System.Threading.Tasks;
using FSH.Framework.Core.Auth.Features.Admin;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Common.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using FSH.Framework.Core.Auth.Domain;

namespace FSH.Starter.Tests.Unit.Features.Admin
{
    public class UpdateUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_UserNotFound_ReturnsFailureAsync()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AppUser)null!);
            var logger = new Mock<ILogger<UpdateUserCommandHandler>>();
            var handler = new UpdateUserCommandHandler(userRepo.Object, logger.Object);
            var emailResult = Email.Create("test@example.com");
            Assert.True(emailResult.IsSuccess);
            var email = emailResult.Value!;
            var usernameResult = Username.Create("testuser");
            Assert.True(usernameResult.IsSuccess);
            var username = usernameResult.Value!;
            var phoneResult = PhoneNumber.Create("5555555555");
            Assert.True(phoneResult.IsSuccess);
            var phone = phoneResult.Value!;
            var firstNameResult = Name.Create("Test");
            Assert.True(firstNameResult.IsSuccess);
            var firstName = firstNameResult.Value!;
            var lastNameResult = Name.Create("User");
            Assert.True(lastNameResult.IsSuccess);
            var lastName = lastNameResult.Value!;
            var command = new UpdateUserCommand { UserId = Guid.NewGuid(), Email = email, Username = username, PhoneNumber = phone, FirstName = firstName, LastName = lastName };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.Error, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
