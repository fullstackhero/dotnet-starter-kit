using FSH.Modules.Identity.Contracts.v1.Users.DeleteUser;
using FSH.Modules.Identity.Features.v1.Users.DeleteUser;
using Identity.Tests.Support;
using Shouldly;
using Xunit;

namespace Identity.Tests.Validators;

public sealed class DeleteUserCommandValidatorTests
{
    private readonly DeleteUserCommandValidator _sut = new(SharedResourcesLocalizerFactory.Create());

    [Fact]
    public void Validate_Should_Pass_When_IdIsProvided()
    {
        // Arrange
        var command = new DeleteUserCommand("user-123");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_IdIsEmpty(string? id)
    {
        // Arrange
        var command = new DeleteUserCommand(id!);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Id" && e.ErrorMessage == "User ID is required.");
    }
}
