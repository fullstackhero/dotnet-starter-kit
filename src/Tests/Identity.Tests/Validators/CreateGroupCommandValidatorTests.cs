using FSH.Modules.Identity.Contracts.v1.Groups.CreateGroup;
using FSH.Modules.Identity.Features.v1.Groups.CreateGroup;

namespace Identity.Tests.Validators;

/// <summary>
/// Tests for CreateGroupCommandValidator - validates group creation requests.
/// </summary>
public sealed class CreateGroupCommandValidatorTests
{
    private readonly CreateGroupCommandValidator _sut = new();

    #region Name Validation

    [Fact]
    public void Name_Should_Pass_When_Valid()
    {
        // Arrange
        var command = new CreateGroupCommand("Developers", null, false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Name_Should_Fail_When_Empty(string? name)
    {
        // Arrange
        var command = new CreateGroupCommand(name!, null, false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Should_Fail_When_ExceedsMaxLength()
    {
        // Arrange
        var longName = new string('a', 257);
        var command = new CreateGroupCommand(longName, null, false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Should_Pass_When_ExactlyMaxLength()
    {
        // Arrange
        var maxLengthName = new string('a', 256);
        var command = new CreateGroupCommand(maxLengthName, null, false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Should_Have_CorrectErrorMessage_When_Empty()
    {
        // Arrange
        var command = new CreateGroupCommand("", null, false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors
            .Where(e => e.PropertyName == "Name")
            .ShouldContain(e => e.ErrorMessage == "Group name is required.");
    }

    [Fact]
    public void Name_Should_Have_CorrectErrorMessage_When_TooLong()
    {
        // Arrange
        var longName = new string('a', 257);
        var command = new CreateGroupCommand(longName, null, false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors
            .Where(e => e.PropertyName == "Name")
            .ShouldContain(e => e.ErrorMessage == "Group name must not exceed 256 characters.");
    }

    #endregion

    #region Description Validation

    [Fact]
    public void Description_Should_Pass_When_Null()
    {
        // Arrange
        var command = new CreateGroupCommand("Developers", null, false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Description_Should_Pass_When_Empty()
    {
        // Arrange
        var command = new CreateGroupCommand("Developers", "", false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Description_Should_Pass_When_Valid()
    {
        // Arrange
        var command = new CreateGroupCommand("Developers", "Software development team", false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Description_Should_Fail_When_ExceedsMaxLength()
    {
        // Arrange
        var longDescription = new string('a', 1025);
        var command = new CreateGroupCommand("Developers", longDescription, false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Description_Should_Pass_When_ExactlyMaxLength()
    {
        // Arrange
        var maxLengthDescription = new string('a', 1024);
        var command = new CreateGroupCommand("Developers", maxLengthDescription, false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Description_Should_Have_CorrectErrorMessage_When_TooLong()
    {
        // Arrange
        var longDescription = new string('a', 1025);
        var command = new CreateGroupCommand("Developers", longDescription, false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors
            .Where(e => e.PropertyName == "Description")
            .ShouldContain(e => e.ErrorMessage == "Description must not exceed 1024 characters.");
    }

    #endregion

    #region Combined Validation

    [Fact]
    public void Validate_Should_Pass_When_AllFieldsValid()
    {
        // Arrange
        var command = new CreateGroupCommand(
            "Engineering Team",
            "All software engineers",
            true,
            ["role-1", "role-2"]);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_Should_Pass_When_OptionalFieldsAreNull()
    {
        // Arrange
        var command = new CreateGroupCommand("Basic Group", null, false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_BothNameAndDescriptionInvalid()
    {
        // Arrange
        var command = new CreateGroupCommand("", new string('a', 1025), false, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(2);
    }

    #endregion
}