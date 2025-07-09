using FSH.Starter.WebApi.Contracts.Admin;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Admin;

public class AssignRoleCommandTests
{
    [Fact]
    public void AssignRoleCommand_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var command = new AssignRoleCommand
        {
            RoleId = "admin"
        };

        // Act & Assert
        Assert.Equal("admin", command.RoleId);
    }

    [Fact]
    public void AssignRoleCommand_DefaultValues_ShouldBeDefault()
    {
        // Arrange & Act
        var command = new AssignRoleCommand();

        // Assert
        Assert.Equal(default, command.RoleId);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("customer_admin")]
    [InlineData("customer_support")]
    [InlineData("base_user")]
    public void AssignRoleCommand_ValidRoles_ShouldSetCorrectly(string role)
    {
        // Arrange & Act
        var command = new AssignRoleCommand { RoleId = role };

        // Assert
        Assert.Equal(role, command.RoleId);
    }
}
