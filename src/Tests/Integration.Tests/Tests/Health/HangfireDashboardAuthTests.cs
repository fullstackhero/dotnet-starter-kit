using System.Text;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Health;

[Collection(FshCollectionDefinition.Name)]
public sealed class HangfireDashboardAuthTests
{
    private const string DashboardPath = "/jobs";
    private const string TestUser = "admin";
    private const string TestPass = "integration-test-hangfire-pwd";

    private readonly FshWebApplicationFactory _factory;

    public HangfireDashboardAuthTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HangfireDashboard_Should_Return401_When_AnonymousRequest()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(DashboardPath);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().ShouldContain("Basic");
    }

    [Fact]
    public async Task HangfireDashboard_Should_Return401_When_WrongCredentials()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", EncodeBasic("attacker", "wrong"));

        var response = await client.GetAsync(DashboardPath);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HangfireDashboard_Should_Allow_When_ValidCredentials()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", EncodeBasic(TestUser, TestPass));

        var response = await client.GetAsync(DashboardPath);

        // Hangfire either renders the dashboard (200) or redirects to its index (301/302).
        ((int)response.StatusCode).ShouldBeInRange(200, 399);
    }

    private static string EncodeBasic(string user, string pass) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
}
