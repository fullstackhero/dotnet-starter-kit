using FSH.Modules.Identity.Contracts.v1.Roles.UpdatePermissions;
using FSH.Modules.Identity.Features.v1.Roles.UpdateRolePermissions;

namespace Identity.Tests.Validators;

/// <summary>
/// Tests for UpdatePermissionsCommandValidator - validates role permission update requests.
/// </summary>
public sealed class UpdatePermissionsCommandValidatorTests
{
    private readonly UpdatePermissionsCommandValidator _sut = new();

    #region RoleId Validation

    [Fact]
    public void RoleId_Should_Pass_When_Valid()
    {
        // Arrange
        var command = new UpdatePermissionsCommand
        {
            RoleId = "admin-role",
            Permissions = ["read", "write"]
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "RoleId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void RoleId_Should_Fail_When_Empty(string? roleId)
    {
        // Arrange
        var command = new UpdatePermissionsCommand
        {
            RoleId = roleId!,
            Permissions = ["read"]
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "RoleId");
    }

    #endregion

    #region Permissions Validation

    [Fact]
    public void Permissions_Should_Pass_When_ValidList()
    {
        // Arrange
        var command = new UpdatePermissionsCommand
        {
            RoleId = "admin-role",
            Permissions = ["Users.View", "Users.Create", "Users.Update"]
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "Permissions");
    }

    [Fact]
    public void Permissions_Should_Pass_When_EmptyList()
    {
        // Arrange - Empty list is valid (removing all permissions)
        var command = new UpdatePermissionsCommand
        {
            RoleId = "admin-role",
            Permissions = []
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "Permissions");
    }

    [Fact]
    public void Permissions_Should_Fail_When_Null()
    {
        // Arrange
        var command = new UpdatePermissionsCommand
        {
            RoleId = "admin-role",
            Permissions = null!
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Permissions");
    }

    #endregion

    #region Combined Validation

    [Fact]
    public void Validate_Should_Pass_When_AllFieldsValid()
    {
        // Arrange
        var command = new UpdatePermissionsCommand
        {
            RoleId = "manager-role",
            Permissions = ["Reports.View", "Reports.Export", "Dashboard.View"]
        };

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
        var command = new UpdatePermissionsCommand
        {
            RoleId = "",
            Permissions = null!
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(2);
    }

    [Fact]
    public void Validate_Should_Pass_WithSinglePermission()
    {
        // Arrange
        var command = new UpdatePermissionsCommand
        {
            RoleId = "basic-role",
            Permissions = ["Dashboard.View"]
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_WithManyPermissions()
    {
        // Arrange
        var command = new UpdatePermissionsCommand
        {
            RoleId = "super-admin",
            Permissions = Enumerable.Range(1, 50).Select(i => $"Permission.{i}").ToList()
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    #endregion
}