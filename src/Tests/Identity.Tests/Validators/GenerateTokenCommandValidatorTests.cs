using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;

namespace Identity.Tests.Validators;

/// <summary>
/// Tests for GenerateTokenCommandValidator - validates login credentials.
/// </summary>
public sealed class GenerateTokenCommandValidatorTests
{
    private readonly GenerateTokenCommandValidator _sut = new();

    #region Email Validation

    [Fact]
    public void Email_Should_Pass_When_Valid()
    {
        // Arrange
        var command = new GenerateTokenCommand("user@example.com", "password123");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Email_Should_Fail_When_Empty(string? email)
    {
        // Arrange
        var command = new GenerateTokenCommand(email!, "password123");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user.example.com")]
    public void Email_Should_Fail_When_InvalidFormat(string email)
    {
        // Arrange
        var command = new GenerateTokenCommand(email, "password123");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.com")]
    [InlineData("user@subdomain.example.com")]
    public void Email_Should_Pass_When_ValidFormat(string email)
    {
        // Arrange
        var command = new GenerateTokenCommand(email, "password123");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "Email");
    }

    #endregion

    #region Password Validation

    [Fact]
    public void Password_Should_Pass_When_Valid()
    {
        // Arrange
        var command = new GenerateTokenCommand("user@example.com", "password123");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Password_Should_Fail_When_Empty(string? password)
    {
        // Arrange
        var command = new GenerateTokenCommand("user@example.com", password!);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }

    #endregion

    #region Combined Validation

    [Fact]
    public void Validate_Should_Pass_When_AllFieldsValid()
    {
        // Arrange
        var command = new GenerateTokenCommand("user@example.com", "SecureP@ssw0rd!");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_Should_Fail_When_AllFieldsInvalid()
    {
        // Arrange
        var command = new GenerateTokenCommand("", "");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    #endregion
}