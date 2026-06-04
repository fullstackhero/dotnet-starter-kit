using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Integration.Tests.Tests.Sessions;

/// <summary>
/// Shared helper for the Identity integration tests that need a *loginable* non-admin
/// user (registered users are <c>IsActive=true</c> but <c>EmailConfirmed=false</c>, so
/// the token endpoint rejects them until the email is confirmed).
///
/// The Finbuckle tenant context MUST be set inline in the same method as the UserManager
/// call: it flows through an AsyncLocal that is lost across an awaited helper boundary,
/// which would NRE inside the multi-tenant query filter.
/// </summary>
internal static class IdentityUserSeeder
{
    /// <summary>
    /// Registers a user via the admin register endpoint, then force-confirms their email
    /// so they can authenticate. Returns the new user's id, email, and password.
    /// The user receives the seeded "Basic" role (Sessions.View + Sessions.Revoke, but
    /// NOT the admin-only Sessions.ViewAll / Sessions.RevokeAll).
    /// </summary>
    public static async Task<SeededUser> CreateLoginableUserAsync(
        FshWebApplicationFactory factory,
        HttpClient adminClient,
        string prefix,
        string tenant = TestConstants.RootTenantId)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}-{uniqueId}@example.com";
        const string password = "Test@1234!";

        var registerResponse = await adminClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/register", new
            {
                firstName = "Seeded",
                lastName = "User",
                email,
                userName = $"{prefix}-{uniqueId}",
                password,
                confirmPassword = password
            });
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResult>();
        registered.ShouldNotBeNull();

        // Force-confirm email via UserManager so the login flow reaches token issuance.
        // Tenant context is set inline (AsyncLocal) right before the UserManager call.
        using var scope = factory.Services.CreateScope();
        var tenantInfo = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(tenant);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenantInfo);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);
        var confirm = await userManager.ConfirmEmailAsync(user!, token);
        confirm.Succeeded.ShouldBeTrue();

        return new SeededUser(registered!.UserId, email, password);
    }
}

internal sealed record SeededUser(string UserId, string Email, string Password);
