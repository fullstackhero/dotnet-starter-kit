using FSH.Starter.WebApi.Contracts.Common;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Common;

public class RoleDtoTests
{
    [Fact]
    public void RoleDto_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var roleDto = new RoleDto
        {
            Id = "role-id-123",
            Name = "Admin",
            Description = "Administrator role with full access"
        };

        // Act & Assert
        Assert.Equal("role-id-123", roleDto.Id);
        Assert.Equal("Admin", roleDto.Name);
        Assert.Equal("Administrator role with full access", roleDto.Description);
    }

    [Fact]
    public void RoleDto_DefaultValues_ShouldBeNull()
    {
        // Arrange & Act
        var roleDto = new RoleDto();

        // Assert
        Assert.Null(roleDto.Id);
        Assert.Null(roleDto.Name);
        Assert.Null(roleDto.Description);
    }
}
