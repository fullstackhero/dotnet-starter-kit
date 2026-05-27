using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Health;

[Collection(FshCollectionDefinition.Name)]
public sealed class HealthCheckTests
{
    private readonly FshWebApplicationFactory _factory;

    public HealthCheckTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LivenessEndpoint_Should_Return200_When_AppIsRunning()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadinessEndpoint_Should_Return200_When_AllDependenciesHealthy()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RootEndpoint_Should_ReturnHelloWorld_When_AppIsRunning()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("hello world");
    }
}
