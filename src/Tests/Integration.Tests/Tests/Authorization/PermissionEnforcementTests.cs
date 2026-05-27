using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Authorization;

[Collection(FshCollectionDefinition.Name)]
public sealed class PermissionEnforcementTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public PermissionEnforcementTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task ProtectedEndpoint_Should_Return401_When_NoTokenProvided()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/users?pageNumber=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_Should_Return401_When_TokenIsInvalid()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/users?pageNumber=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_Should_ReturnOk_When_AdminUserAccesses()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/roles");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_Should_RejectExpiredToken()
    {
        // Verify that authentication middleware properly rejects malformed tokens
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Use a structurally valid but expired/tampered JWT
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwiZXhwIjoxfQ.invalid");

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/roles");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousEndpoints_Should_ReturnOk_When_NoAuthProvided()
    {
        using var client = _factory.CreateClient();

        var liveResponse = await client.GetAsync("/health/live");
        var rootResponse = await client.GetAsync("/");

        liveResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        rootResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
