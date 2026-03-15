using System.Security.Claims;
using FSH.Framework.Shared.Identity.Claims;
using Xunit;

namespace Generic.Tests.Identity;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_Should_ReturnUidClaim_WhenPresent()
    {
        // Arrange
        var claims = new List<Claim> { new Claim("uid", "user-123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var userId = principal.GetUserId();

        // Assert
        Assert.Equal("user-123", userId);
    }

    [Fact]
    public void GetUserId_Should_ReturnSubClaim_WhenUidIsMissing()
    {
        // Arrange
        var claims = new List<Claim> { new Claim("sub", "user-456") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var userId = principal.GetUserId();

        // Assert
        Assert.Equal("user-456", userId);
    }

    [Fact]
    public void GetUserId_Should_ReturnNameIdentifier_WhenUidAndSubAreMissing()
    {
        // Arrange
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user-789") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var userId = principal.GetUserId();

        // Assert
        Assert.Equal("user-789", userId);
    }

    [Fact]
    public void GetUserId_Should_PrioritizeUidOverSub()
    {
        // Arrange
        var claims = new List<Claim> 
        { 
            new Claim("uid", "priority-uid"),
            new Claim("sub", "fallback-sub")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var userId = principal.GetUserId();

        // Assert
        Assert.Equal("priority-uid", userId);
    }
}
