using FSH.Modules.Identity.Contracts.v1.Roles.UpsertRole;
using FSH.Modules.Identity.Features.v1.Roles.UpsertRole;

namespace Identity.Tests.Validators;

/// <summary>
/// Tests for UpsertRoleCommandValidator - validates role creation/update requests.
/// </summary>
public sealed class UpsertRoleCommandValidatorTests
{
    private readonly UpsertRoleCommandValidator _sut = new();

    #region Name Validation

    [Fact]
    public void Name_Should_Pass_When_Valid()
    {
        // Arrange
        var command = new UpsertRoleCommand { Id = "role-1", Name = "Admin" };

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
        var command = new UpsertRoleCommand { Id = "role-1", Name = name! };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Should_Have_CorrectErrorMessage()
    {
        // Arrange
        var command = new UpsertRoleCommand { Id = "role-1", Name = "" };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors
            .Where(e => e.PropertyName == "Name")
            .ShouldContain(e => e.ErrorMessage == "Role name is required.");
    }

    #endregion

    #region Combined Validation

    [Fact]
    public void Validate_Should_Pass_When_NameProvided()
    {
        // Arrange
        var command = new UpsertRoleCommand
        {
            Id = "role-1",
            Name = "Manager",
            Description = "Manager role with elevated permissions"
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_Should_Pass_When_DescriptionIsNull()
    {
        // Arrange
        var command = new UpsertRoleCommand
        {
            Id = "role-1",
            Name = "User",
            Description = null
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Super Admin")]
    [InlineData("Read-Only User")]
    [InlineData("API_Access")]
    public void Validate_Should_Pass_ForVariousRoleNames(string roleName)
    {
        // Arrange
        var command = new UpsertRoleCommand { Id = "role-1", Name = roleName };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    #endregion
}