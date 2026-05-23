using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Integration.Tests.Tests.Middleware;

/// <summary>
/// Exercises the PRODUCTION <c>SecurityHeadersMiddleware</c> which the default test host
/// disables via <c>SecurityHeadersOptions:Enabled=false</c>.
///
/// Re-wiring technique: a derived factory created with <see cref="WebApplicationFactory{TEntryPoint}.WithWebHostBuilder"/>
/// flips <c>SecurityHeadersOptions:Enabled</c> back to <c>true</c> through an in-memory configuration
/// overlay added LAST (so it wins over the base factory's overlay). The middleware itself is already
/// wired into the host pipeline by <c>UseHeroPlatform</c> (<c>app.UseHeroSecurityHeaders()</c>); only its
/// options gate is disabled in tests, so re-enabling the option is all that is required to drive the
/// real middleware.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class SecurityHeadersMiddlewareTests
{
    private readonly FshWebApplicationFactory _factory;

    public SecurityHeadersMiddlewareTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateFactoryWithSecurityHeadersEnabled() =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SecurityHeadersOptions:Enabled"] = "true",
                });
            });
        });

    #region Happy Path

    [Fact]
    public async Task SecurityHeaders_Should_BePresentOnNormalResponse_When_MiddlewareEnabled()
    {
        // Arrange
        await using var factory = CreateFactoryWithSecurityHeadersEnabled();
        using var client = factory.CreateClient();

        // Act — the root endpoint is anonymous and not in ExcludedPaths, so the middleware runs in full.
        var response = await client.GetAsync("/");

        // Assert — production security headers set by SecurityHeadersMiddleware.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        response.Headers.GetValues("X-Frame-Options").ShouldContain("DENY");
        response.Headers.GetValues("Referrer-Policy").ShouldContain("strict-origin-when-cross-origin");
        response.Headers.GetValues("X-XSS-Protection").ShouldContain("0");

        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        csp.ShouldContain("default-src 'self'");
        csp.ShouldContain("frame-ancestors 'none'");
        csp.ShouldContain("object-src 'none'");
        csp.ShouldContain("base-uri 'self'");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SecurityHeaders_Should_BeAbsent_When_MiddlewareDisabledByDefault()
    {
        // Arrange — the un-modified base factory has SecurityHeadersOptions:Enabled=false.
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert — disabled middleware short-circuits without writing any header,
        // proving the option gate (not some other layer) is what produces the headers above.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("X-Content-Type-Options").ShouldBeFalse();
        response.Headers.Contains("X-Frame-Options").ShouldBeFalse();
        response.Headers.Contains("Content-Security-Policy").ShouldBeFalse();
    }

    #endregion
}
