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

namespace FSH.Starter.Tests.Unit.Features.Admin
{
    public class CreateUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_EmailAlreadyExists_ReturnsFailureAsync()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>())).ReturnsAsync(true);
            var logger = new Mock<ILogger<CreateUserCommandHandler>>();
            var handler = new CreateUserCommandHandler(userRepo.Object, logger.Object);
            var emailResult = Email.Create("test@example.com");
            Assert.True(emailResult.IsSuccess);
            var email = emailResult.Value!;
            var usernameResult = Username.Create("testuser");
            Assert.True(usernameResult.IsSuccess);
            var username = usernameResult.Value!;
            var phoneResult = PhoneNumber.Create("5555555555");
            Assert.True(phoneResult.IsSuccess);
            var phone = phoneResult.Value!;
            var tcknResult = Tckn.Create("10000000146");
            Assert.True(tcknResult.IsSuccess);
            var tckn = tcknResult.Value!;
            var firstNameResult = Name.Create("Test");
            Assert.True(firstNameResult.IsSuccess);
            var firstName = firstNameResult.Value!;
            var lastNameResult = Name.Create("User");
            Assert.True(lastNameResult.IsSuccess);
            var lastName = lastNameResult.Value!;
            var birthDateResult = BirthDate.Create(DateTime.UtcNow.AddYears(-30));
            Assert.True(birthDateResult.IsSuccess);
            var birthDate = birthDateResult.Value!;
            var command = new CreateUserCommand { Email = email, Username = username, PhoneNumber = phone, Tckn = tckn, FirstName = firstName, LastName = lastName, BirthDate = birthDate };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal($"User with email {email.Value} already exists", result.Error);
        }

        [Fact]
        public async Task Handle_UsernameAlreadyExists_ReturnsFailureAsync()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>())).ReturnsAsync(false);
            userRepo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>())).ReturnsAsync(true);
            var logger = new Mock<ILogger<CreateUserCommandHandler>>();
            var handler = new CreateUserCommandHandler(userRepo.Object, logger.Object);
            var emailResult = Email.Create("test@example.com");
            Assert.True(emailResult.IsSuccess);
            var email = emailResult.Value!;
            var usernameResult = Username.Create("testuser");
            Assert.True(usernameResult.IsSuccess);
            var username = usernameResult.Value!;
            var phoneResult = PhoneNumber.Create("5555555555");
            Assert.True(phoneResult.IsSuccess);
            var phone = phoneResult.Value!;
            var tcknResult = Tckn.Create("10000000146");
            Assert.True(tcknResult.IsSuccess);
            var tckn = tcknResult.Value!;
            var firstNameResult = Name.Create("Test");
            Assert.True(firstNameResult.IsSuccess);
            var firstName = firstNameResult.Value!;
            var lastNameResult = Name.Create("User");
            Assert.True(lastNameResult.IsSuccess);
            var lastName = lastNameResult.Value!;
            var birthDateResult = BirthDate.Create(DateTime.UtcNow.AddYears(-30));
            Assert.True(birthDateResult.IsSuccess);
            var birthDate = birthDateResult.Value!;
            var command = new CreateUserCommand { Email = email, Username = username, PhoneNumber = phone, Tckn = tckn, FirstName = firstName, LastName = lastName, BirthDate = birthDate };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal($"User with username {username.Value} already exists", result.Error);
        }
    }
}
