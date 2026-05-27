using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Modules.Identity.Services;
using System.Security.Claims;

namespace Identity.Tests.Services;

/// <summary>
/// Tests for CurrentUserService - handles current user context.
/// </summary>
public sealed class CurrentUserServiceTests
{
    private static ClaimsPrincipal CreateAuthenticatedPrincipal(
        string userId,
        string? email = null,
        string? name = null,
        string? tenant = null,
        params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };

        if (email != null)
            claims.Add(new Claim(ClaimTypes.Email, email));
        if (name != null)
            claims.Add(new Claim(ClaimTypes.Name, name));
        if (tenant != null)
            claims.Add(new Claim(CustomClaims.Tenant, tenant));

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateUnauthenticatedPrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    #region GetUserId Tests

    [Fact]
    public void GetUserId_Should_ReturnGuid_When_Authenticated()
    {
        // Arrange
        var service = new CurrentUserService();
        var userId = Guid.NewGuid();
        var principal = CreateAuthenticatedPrincipal(userId.ToString());
        service.SetCurrentUser(principal);

        // Act
        var result = service.GetUserId();

        // Assert
        result.ShouldBe(userId);
    }

    [Fact]
    public void GetUserId_Should_ReturnStoredId_When_NotAuthenticated()
    {
        // Arrange
        var service = new CurrentUserService();
        var userId = Guid.NewGuid();
        service.SetCurrentUserId(userId.ToString());

        // Act
        var result = service.GetUserId();

        // Assert
        result.ShouldBe(userId);
    }

    [Fact]
    public void GetUserId_Should_ReturnEmptyGuid_When_NoSource()
    {
        // Arrange
        var service = new CurrentUserService();

        // Act
        var result = service.GetUserId();

        // Assert
        result.ShouldBe(Guid.Empty);
    }

    #endregion

    #region GetUserEmail Tests

    [Fact]
    public void GetUserEmail_Should_ReturnEmail_When_Authenticated()
    {
        // Arrange
        var service = new CurrentUserService();
        var principal = CreateAuthenticatedPrincipal(
            Guid.NewGuid().ToString(),
            email: "test@example.com");
        service.SetCurrentUser(principal);

        // Act
        var result = service.GetUserEmail();

        // Assert
        result.ShouldBe("test@example.com");
    }

    [Fact]
    public void GetUserEmail_Should_ReturnEmpty_When_NotAuthenticated()
    {
        // Arrange
        var service = new CurrentUserService();
        var principal = CreateUnauthenticatedPrincipal();
        service.SetCurrentUser(principal);

        // Act
        var result = service.GetUserEmail();

        // Assert
        result.ShouldBe(string.Empty);
    }

    #endregion

    #region IsAuthenticated Tests

    [Fact]
    public void IsAuthenticated_Should_ReturnTrue_When_UserAuthenticated()
    {
        // Arrange
        var service = new CurrentUserService();
        var principal = CreateAuthenticatedPrincipal(Guid.NewGuid().ToString());
        service.SetCurrentUser(principal);

        // Act
        var result = service.IsAuthenticated();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsAuthenticated_Should_ReturnFalse_When_UserNotAuthenticated()
    {
        // Arrange
        var service = new CurrentUserService();
        var principal = CreateUnauthenticatedPrincipal();
        service.SetCurrentUser(principal);

        // Act
        var result = service.IsAuthenticated();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAuthenticated_Should_ReturnFalse_When_NoPrincipalSet()
    {
        // Arrange
        var service = new CurrentUserService();

        // Act
        var result = service.IsAuthenticated();

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsInRole Tests

    [Fact]
    public void IsInRole_Should_ReturnTrue_When_UserHasRole()
    {
        // Arrange
        var service = new CurrentUserService();
        var principal = CreateAuthenticatedPrincipal(
            Guid.NewGuid().ToString(),
            null, null, null,
            "Admin", "User");
        service.SetCurrentUser(principal);

        // Act
        var result = service.IsInRole("Admin");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsInRole_Should_ReturnFalse_When_UserLacksRole()
    {
        // Arrange
        var service = new CurrentUserService();
        var principal = CreateAuthenticatedPrincipal(
            Guid.NewGuid().ToString(),
            null, null, null,
            "User");
        service.SetCurrentUser(principal);

        // Act
        var result = service.IsInRole("Admin");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsInRole_Should_ReturnFalse_When_NoPrincipal()
    {
        // Arrange
        var service = new CurrentUserService();

        // Act
        var result = service.IsInRole("Admin");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region GetUserClaims Tests

    [Fact]
    public void GetUserClaims_Should_ReturnAllClaims_When_Authenticated()
    {
        // Arrange
        var service = new CurrentUserService();
        var principal = CreateAuthenticatedPrincipal(
            Guid.NewGuid().ToString(),
            email: "test@example.com",
            name: "Test User");
        service.SetCurrentUser(principal);

        // Act
        var result = service.GetUserClaims();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain(c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
        result.ShouldContain(c => c.Type == ClaimTypes.Name && c.Value == "Test User");
    }

    [Fact]
    public void GetUserClaims_Should_ReturnNull_When_NoPrincipal()
    {
        // Arrange
        var service = new CurrentUserService();

        // Act
        var result = service.GetUserClaims();

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetTenant Tests

    [Fact]
    public void GetTenant_Should_ReturnTenant_When_Authenticated()
    {
        // Arrange
        var service = new CurrentUserService();
        var principal = CreateAuthenticatedPrincipal(
            Guid.NewGuid().ToString(),
            tenant: "tenant-1");
        service.SetCurrentUser(principal);

        // Act
        var result = service.GetTenant();

        // Assert
        result.ShouldBe("tenant-1");
    }

    [Fact]
    public void GetTenant_Should_ReturnEmpty_When_NotAuthenticated()
    {
        // Arrange
        var service = new CurrentUserService();
        var principal = CreateUnauthenticatedPrincipal();
        service.SetCurrentUser(principal);

        // Act
        var result = service.GetTenant();

        // Assert
        result.ShouldBe(string.Empty);
    }

    #endregion

    #region SetCurrentUser Tests

    [Fact]
    public void SetCurrentUser_Should_Throw_When_CalledTwice()
    {
        // Arrange
        var service = new CurrentUserService();
        var principal = CreateAuthenticatedPrincipal(Guid.NewGuid().ToString());
        service.SetCurrentUser(principal);

        // Act & Assert
        Should.Throw<CustomException>(() =>
            service.SetCurrentUser(principal));
    }

    [Fact]
    public void SetCurrentUser_Should_StorePrincipal()
    {
        // Arrange
        var service = new CurrentUserService();
        var principal = CreateAuthenticatedPrincipal(Guid.NewGuid().ToString());

        // Act
        service.SetCurrentUser(principal);

        // Assert
        service.IsAuthenticated().ShouldBeTrue();
    }

    #endregion

    #region SetCurrentUserId Tests

    [Fact]
    public void SetCurrentUserId_Should_Throw_When_CalledTwice()
    {
        // Arrange
        var service = new CurrentUserService();
        var userId = Guid.NewGuid().ToString();
        service.SetCurrentUserId(userId);

        // Act & Assert
        Should.Throw<CustomException>(() =>
            service.SetCurrentUserId(userId));
    }

    [Fact]
    public void SetCurrentUserId_Should_NotThrow_When_NullOrEmpty()
    {
        // Arrange
        var service = new CurrentUserService();

        // Act & Assert - Should not throw
        Should.NotThrow(() => service.SetCurrentUserId(null!));
        Should.NotThrow(() => service.SetCurrentUserId(string.Empty));
    }

    [Fact]
    public void SetCurrentUserId_Should_ParseAndStoreGuid()
    {
        // Arrange
        var service = new CurrentUserService();
        var userId = Guid.NewGuid();

        // Act
        service.SetCurrentUserId(userId.ToString());

        // Assert
        service.GetUserId().ShouldBe(userId);
    }

    #endregion

    #region Name Property Tests

    [Fact]
    public void Name_Should_ReturnIdentityName_When_Set()
    {
        // Arrange
        var service = new CurrentUserService();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, "John Doe")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType", ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        service.SetCurrentUser(principal);

        // Act
        var result = service.Name;

        // Assert
        result.ShouldBe("John Doe");
    }

    [Fact]
    public void Name_Should_ReturnNull_When_NoPrincipal()
    {
        // Arrange
        var service = new CurrentUserService();

        // Act
        var result = service.Name;

        // Assert
        result.ShouldBeNull();
    }

    #endregion
}