using FSH.Modules.Identity.Contracts.v1.Users.ResetPassword;
using FSH.Modules.Identity.Features.v1.Users.ResetPassword;
using Shouldly;
using Xunit;

namespace Identity.Tests.Validators;

public sealed class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_When_AllFieldsValid()
    {
        // Arrange
        var command = new ResetPasswordCommand 
        { 
            Email = "test@example.com", 
            Password = "Password123!", 
            Token = "valid-token" 
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12345")]
    public void Validate_Should_Fail_When_PasswordIsTooShort(string password)
    {
        // Arrange
        var command = new ResetPasswordCommand 
        { 
            Email = "test@example.com", 
            Password = password, 
            Token = "token" 
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_Should_Fail_When_EmailIsInvalid()
    {
        // Arrange
        var command = new ResetPasswordCommand 
        { 
            Email = "not-an-email", 
            Password = "Password123!", 
            Token = "token" 
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_Should_Fail_When_TokenIsEmpty()
    {
        // Arrange
        var command = new ResetPasswordCommand 
        { 
            Email = "test@example.com", 
            Password = "Password123!", 
            Token = "" 
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Token");
    }
}
