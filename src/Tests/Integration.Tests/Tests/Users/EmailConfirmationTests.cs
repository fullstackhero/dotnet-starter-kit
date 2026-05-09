using Integration.Tests.Infrastructure;
using FSH.Modules.Identity.Contracts.v1.Users.RegisterUser;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using Shouldly;
using Xunit;
using FSH.Modules.Identity.Contracts.v1.Users.ConfirmEmail;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Integration.Tests.Tests.Users;

[Collection(FshCollectionDefinition.Name)]
public sealed class EmailConfirmationTests
{
    private readonly FshWebApplicationFactory _factory;

    public EmailConfirmationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConfirmEmail_Should_ActivateUser_When_ValidTokenIsProvided()
    {
        // Arrange - Create a user via RegisterUser (this user will have EmailConfirmed = false)
        using var scope = _factory.Services.CreateScope();
        
        // Set tenant context for UserManager
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var email = $"newuser_{Guid.NewGuid()}@test.com";
        var user = new FshUser
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            UserName = email.Split('@')[0],
            EmailConfirmed = false
        };

        var createResult = await userManager.CreateAsync(user, TestConstants.DefaultPassword);
        createResult.Succeeded.ShouldBeTrue();

        // Generate confirmation token and encode it as the API expects
        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        // Act - Call confirm-email endpoint
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/confirm-email?userId={user.Id}&code={Uri.EscapeDataString(encodedCode)}&tenant={TestConstants.RootTenantId}");

        // Assert
        var errorContent = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Response content: {errorContent}");
        
        // Verify user is now confirmed in a fresh scope to avoid stale EF cache
        using var assertScope = _factory.Services.CreateScope();
        var assertTenant = await assertScope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(TestConstants.RootTenantId);
        assertScope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext = new MultiTenantContext<AppTenantInfo>(assertTenant);
        var assertUserManager = assertScope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        
        var updatedUser = await assertUserManager.FindByIdAsync(user.Id.ToString());
        updatedUser.ShouldNotBeNull();
        updatedUser.EmailConfirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task ConfirmEmail_Should_Fail_When_InvalidTokenIsProvided()
    {
        // Arrange - Create a user
        using var scope = _factory.Services.CreateScope();

        // Set tenant context for UserManager
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var email = $"invalid_{Guid.NewGuid()}@test.com";
        var user = new FshUser
        {
            FirstName = "Invalid",
            LastName = "Token",
            Email = email,
            UserName = email.Split('@')[0],
            EmailConfirmed = false
        };

        await userManager.CreateAsync(user, TestConstants.DefaultPassword);

        // Act - Call confirm-email with invalid code
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var invalidCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("invalid-code"));
        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/confirm-email?userId={user.Id}&code={invalidCode}&tenant={TestConstants.RootTenantId}");

        // Assert
        // The endpoint currently returns Ok(result) where result is the message from UserService.
        // Let's check what UserService.ConfirmEmailAsync returns on failure.
        var message = await response.Content.ReadAsStringAsync();
        message.ShouldContain("Error", Case.Insensitive);

        // Verify user is still NOT confirmed
        var updatedUser = await userManager.FindByIdAsync(user.Id.ToString());
        updatedUser?.EmailConfirmed.ShouldBeFalse();
    }
}
