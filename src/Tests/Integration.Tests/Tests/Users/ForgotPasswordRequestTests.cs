using Integration.Tests.Infrastructure;
using Integration.Tests.Tests.Sessions;

namespace Integration.Tests.Tests.Users;

/// <summary>
/// Covers the POST /forgot-password REQUEST flow (ForgotPasswordCommandHandler).
/// The endpoint is anonymous and tenant-scoped (tenant header required). Mail
/// dispatch is a no-op in tests (NoOpMailService), so we assert the HTTP contract,
/// not the email contents.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ForgotPasswordRequestTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public ForgotPasswordRequestTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task ForgotPassword_Should_ReturnOk_When_EmailBelongsToExistingUser()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "forgot-known");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/forgot-password", new { email = user.Email });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_Should_Return400_When_EmailIsEmpty()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/forgot-password", new { email = "" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_Should_Return400_When_EmailIsMalformed()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/forgot-password", new { email = "not-an-email" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_Should_ReturnUniformOk_When_EmailIsUnknown()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/forgot-password",
            new { email = $"definitely-missing-{Guid.NewGuid():N}@example.com" });

        // Assert — should be indistinguishable from the known-email path.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
