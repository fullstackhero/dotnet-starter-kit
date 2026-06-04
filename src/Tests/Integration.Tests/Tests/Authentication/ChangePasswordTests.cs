using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Integration.Tests.Tests.Authentication;

/// <summary>
/// Authenticated self-service password change. Proves the change takes effect
/// end-to-end (new password authenticates, old one is rejected) and that a wrong
/// current password is refused without mutating anything.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ChangePasswordTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public ChangePasswordTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task ChangePassword_Should_SetNewPassword_When_CurrentPasswordIsCorrect()
    {
        // Arrange
        var email = $"changepw_ok_{Guid.NewGuid():N}@test.com";
        const string oldPassword = TestConstants.DefaultPassword;
        const string newPassword = "NewPa$$word123!";
        await CreateActiveUserAsync(email, oldPassword);
        using var client = await _auth.CreateAuthenticatedClientAsync(email, oldPassword, TestConstants.RootTenantId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/change-password",
            new { password = oldPassword, newPassword, confirmNewPassword = newPassword });

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);

        var token = await _auth.GetTokenAsync(email, newPassword, TestConstants.RootTenantId);
        token.AccessToken.ShouldNotBeNullOrWhiteSpace();

        await Should.ThrowAsync<HttpRequestException>(
            () => _auth.GetTokenAsync(email, oldPassword, TestConstants.RootTenantId));
    }

    #endregion

    #region Exception / Edge Cases

    [Fact]
    public async Task ChangePassword_Should_Fail_And_LeavePasswordUnchanged_When_CurrentPasswordIsWrong()
    {
        // Arrange
        var email = $"changepw_wrong_{Guid.NewGuid():N}@test.com";
        const string oldPassword = TestConstants.DefaultPassword;
        await CreateActiveUserAsync(email, oldPassword);
        using var client = await _auth.CreateAuthenticatedClientAsync(email, oldPassword, TestConstants.RootTenantId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/change-password",
            new { password = "WrongCurrent123!", newPassword = "NewPa$$word123!", confirmNewPassword = "NewPa$$word123!" });

        // Assert — refused, and the original password still authenticates.
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        var token = await _auth.GetTokenAsync(email, oldPassword, TestConstants.RootTenantId);
        token.AccessToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ChangePassword_Should_Return401_When_Unauthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/change-password",
            new { password = "x", newPassword = "NewPa$$word123!", confirmNewPassword = "NewPa$$word123!" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helpers

    private async Task CreateActiveUserAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var user = new FshUser
        {
            FirstName = "Change",
            LastName = "User",
            Email = email,
            UserName = email.Split('@')[0],
            EmailConfirmed = true,
            IsActive = true
        };
        var result = await userManager.CreateAsync(user, password);
        result.Succeeded.ShouldBeTrue(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    #endregion
}
