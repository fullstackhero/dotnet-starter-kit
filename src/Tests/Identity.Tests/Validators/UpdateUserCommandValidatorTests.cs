using FSH.Modules.Identity.Contracts.v1.Users.UpdateUser;
using FSH.Modules.Identity.Features.v1.Users.UpdateUser;
using Shouldly;
using Xunit;

namespace Identity.Tests.Validators;

public sealed class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_When_ValidMinimalCommand()
    {
        // Arrange
        var command = new UpdateUserCommand { Id = "user-123" };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_IdIsEmpty()
    {
        // Arrange
        var command = new UpdateUserCommand { Id = "" };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_Should_Fail_When_FirstNameExceedsMaxLength()
    {
        // Arrange
        var command = new UpdateUserCommand 
        { 
            Id = "user-123", 
            FirstName = new string('a', 51) 
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void Validate_Should_Fail_When_EmailIsInvalid()
    {
        // Arrange
        var command = new UpdateUserCommand 
        { 
            Id = "user-123", 
            Email = "not-an-email" 
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_Should_Fail_When_DeleteImageAndUploadImage_Simultaneously()
    {
        // Arrange
        var command = new UpdateUserCommand 
        { 
            Id = "user-123", 
            DeleteCurrentImage = true, 
            Image = new FSH.Framework.Shared.Storage.FileUploadRequest { FileName = "test.png", Data = [0] } 
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "You cannot upload a new image and delete the current one simultaneously.");
    }
}
