using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Authentication;

[Collection(FshCollectionDefinition.Name)]
public sealed class AccountLockoutTests
{
    private const int MaxFailedAttempts = 5;
    private const HttpStatusCode HttpLocked = (HttpStatusCode)423;

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public AccountLockoutTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Login_Should_LockAccount_After_ConsecutiveFailedAttempts()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var (email, _) = await CreateConfirmedUserAsync(rootClient);

        // Five wrong-password attempts must all return 401 (generic auth failure).
        for (int attempt = 1; attempt <= MaxFailedAttempts; attempt++)
        {
            var response = await AttemptLoginAsync(email, "WrongPassword!1");
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized,
                $"Attempt {attempt} should return 401 before the lockout threshold is crossed.");
        }

        // Subsequent attempts (even with the CORRECT password) must be rejected with 423 Locked.
        var afterLockout = await AttemptLoginAsync(email, "Test@1234!");
        afterLockout.StatusCode.ShouldBe(HttpLocked);
    }

    [Fact]
    public async Task Login_Should_ResetFailedCount_After_SuccessfulAuthentication()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var (email, password) = await CreateConfirmedUserAsync(rootClient);

        // Three wrong attempts — below the 5-attempt threshold.
        for (int i = 0; i < 3; i++)
        {
            (await AttemptLoginAsync(email, "WrongPassword!1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // A correct login must succeed AND reset the counter.
        var successResponse = await AttemptLoginAsync(email, password);
        successResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // After the reset, another 4 wrong attempts should still only yield 401 — not 423.
        // If the counter weren't reset, attempt 2 below would cross the threshold and lock.
        for (int i = 0; i < 4; i++)
        {
            (await AttemptLoginAsync(email, "WrongPassword!1")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    private async Task<(string Email, string Password)> CreateConfirmedUserAsync(HttpClient rootClient)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"lockout-{uniqueId}@example.com";
        const string password = "Test@1234!";

        // Register via admin path so we have a known user.
        var registerResponse = await rootClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/register", new
            {
                firstName = "Lockout",
                lastName = "Test",
                email,
                userName = $"lockoutuser-{uniqueId}",
                password,
                confirmPassword = password
            });
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Admin-registered users aren't auto-confirmed; force-confirm via UserManager
        // so the login flow reaches the password + lockout check instead of the
        // EmailConfirmed guard. Need to set tenant context before UserManager queries
        // so Finbuckle's multi-tenant filter has a real TenantInfo.
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<FSH.Modules.Identity.Domain.FshUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);
        var confirm = await userManager.ConfirmEmailAsync(user!, token);
        confirm.Succeeded.ShouldBeTrue();

        return (email, password);
    }

    private async Task<HttpResponseMessage> AttemptLoginAsync(string email, string password)
    {
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
        request.Headers.Add("tenant", TestConstants.RootTenantId);
        request.Content = JsonContent.Create(new { email, password });
        return await client.SendAsync(request);
    }
}
