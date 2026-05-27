using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Authentication;

[Collection(FshCollectionDefinition.Name)]
public sealed class AuthRateLimitWiringTests
{
    private const string AuthPolicy = "auth";

    // Each entry: route-pattern suffix that uniquely identifies the endpoint.
    private static readonly string[] AuthSensitiveRouteSuffixes =
    {
        "token/issue",
        "token/refresh",
        "forgot-password",
        "reset-password",
        "confirm-email",
        "self-register"
    };

    private readonly FshWebApplicationFactory _factory;

    public AuthRateLimitWiringTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void AuthSensitiveEndpoints_Should_AllHaveAuthRateLimitPolicyAttached()
    {
        _ = _factory.Server;

        var dataSource = _factory.Services.GetRequiredService<EndpointDataSource>();
        var endpoints = dataSource.Endpoints.OfType<RouteEndpoint>().ToList();

        var missing = new List<string>();

        foreach (var suffix in AuthSensitiveRouteSuffixes)
        {
            var matches = endpoints
                .Where(e => e.RoutePattern.RawText is { } raw &&
                            raw.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                missing.Add($"{suffix} (no matching endpoint registered)");
                continue;
            }

            var anyHasAuthPolicy = matches.Any(e =>
            {
                var policy = e.Metadata.GetMetadata<EnableRateLimitingAttribute>();
                return policy is not null
                    && string.Equals(policy.PolicyName, AuthPolicy, StringComparison.Ordinal);
            });

            if (!anyHasAuthPolicy)
            {
                missing.Add($"{suffix} (no '{AuthPolicy}' rate limit policy on any matching endpoint)");
            }
        }

        missing.ShouldBeEmpty(
            $"Auth-sensitive endpoints missing rate limiting:\n  - {string.Join("\n  - ", missing)}");
    }
}
