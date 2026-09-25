using FSH.Framework.Web.Cors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AspNetCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;

namespace Framework.Tests.Web;

public sealed class CorsPolicyTests
{
    private const string PolicyName = "FSHCorsPolicy";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Policy_Should_ExposeETag_When_Built(bool allowAll)
    {
        // Arrange — ETag is not a CORS-safelisted response header, so a front-end can only read the
        // concurrency validator (and answer with If-Match) if the policy exposes it explicitly.
        // Both branches are covered: the restricted one builds from configured lists, and neither
        // AllowAnyHeader nor WithHeaders implies exposure.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CorsOptions:AllowAll"] = allowAll ? "true" : "false",
                ["CorsOptions:AllowedOrigins:0"] = "https://app.example.com",
                ["CorsOptions:AllowedHeaders:0"] = "content-type",
                ["CorsOptions:AllowedMethods:0"] = "GET"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddHeroCors(configuration);

        // Act
        var policy = services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<AspNetCorsOptions>>()
            .Value
            .GetPolicy(PolicyName);

        // Assert
        policy.ShouldNotBeNull();
        policy!.ExposedHeaders.ShouldContain("ETag");
    }
}
