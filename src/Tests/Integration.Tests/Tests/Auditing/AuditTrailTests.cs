using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Auditing;

[Collection(FshCollectionDefinition.Name)]
public sealed class AuditTrailTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public AuditTrailTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetAudits_Should_ReturnOk_When_Authenticated()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync($"{TestConstants.AuditsBasePath}?pageNumber=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSecurityAudits_Should_ReturnOk_When_LoginEventsExist()
    {
        // Generate a login event
        await _auth.GetRootAdminTokenAsync();
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync(
            $"{TestConstants.AuditsBasePath}/security?pageNumber=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAuditSummary_Should_ReturnOk_When_Authenticated()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync($"{TestConstants.AuditsBasePath}/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAudits_Should_Return401_When_NotAuthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.GetAsync($"{TestConstants.AuditsBasePath}?pageNumber=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
