using FSH.Modules.Identity.Contracts.v1.Users.ForgotPassword;
using FSH.Modules.Identity.Features.v1.Users.ForgotPassword;
using Shouldly;
using Xunit;

namespace Identity.Tests.Validators;

public sealed class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_When_EmailIsValid()
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = "test@example.com" };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_EmailIsEmpty(string? email)
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = email! };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    public void Validate_Should_Fail_When_EmailIsInvalid(string email)
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = email };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }
}
