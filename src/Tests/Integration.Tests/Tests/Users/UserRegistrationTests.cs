using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Users;

[Collection(FshCollectionDefinition.Name)]
public sealed class UserRegistrationTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public UserRegistrationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task RegisterUser_Should_Return201WithUserId_When_DataIsValid()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "Test",
            lastName = "User",
            email = $"test-{uniqueId}@example.com",
            userName = $"testuser-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.DeserializeAsync<RegisterResult>();
        result.UserId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RegisterUser_Should_RejectDuplicate_When_EmailAlreadyExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            firstName = "Dup",
            lastName = "User",
            email = $"dup-{uniqueId}@example.com",
            userName = $"dupuser-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        };

        var firstResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", payload);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var secondResponse = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/register",
            payload with { userName = $"dupuser2-{uniqueId}" });

        secondResponse.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task RegisterUser_Should_Reject_When_PasswordTooWeak()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "Weak",
            lastName = "Pass",
            email = $"weak-{uniqueId}@example.com",
            userName = $"weakuser-{uniqueId}",
            password = "123",
            confirmPassword = "123"
        });

        response.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task RegisterUser_Should_Return401_When_NotAuthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "NoAuth",
            lastName = "User",
            email = "noauth@example.com",
            userName = "noauthuser",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterUser_Should_RequireEmailConfirmation_When_RegisteredByAdmin()
    {
        // Admin-registered users have EmailConfirmed = false and cannot login immediately
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        string email = $"login-{uniqueId}@example.com";

        var registerResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "Login",
            lastName = "Test",
            email,
            userName = $"loginuser-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        });
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Attempt login — should fail because email is not confirmed
        using var loginClient = _factory.CreateClient();
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
        loginRequest.Headers.Add("tenant", TestConstants.RootTenantId);
        loginRequest.Content = JsonContent.Create(new { email, password = "Test@1234!" });

        var loginResponse = await loginClient.SendAsync(loginRequest);

        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
