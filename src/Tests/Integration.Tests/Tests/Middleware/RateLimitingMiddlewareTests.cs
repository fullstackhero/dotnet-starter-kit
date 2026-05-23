using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Integration.Tests.Tests.Middleware;

/// <summary>
/// Exercises the PRODUCTION rate limiter (<c>FSH.Framework.Web.RateLimiting</c>) which the default
/// test host DISABLES via <c>RateLimitingOptions:Enabled=false</c>.
///
/// Re-wiring technique: a derived factory (<see cref="WebApplicationFactory{TEntryPoint}.WithWebHostBuilder"/>)
/// adds an in-memory configuration overlay LAST that flips <c>RateLimitingOptions:Enabled</c> back to
/// <c>true</c> and clamps the <c>auth</c> policy to a tiny fixed window. <c>AddHeroRateLimiting</c> reads
/// <c>RateLimitingOptions</c> EAGERLY at service-registration time, so the override has to live in
/// configuration (it must be present before <c>AddHeroPlatform</c> runs) rather than as a post-build
/// service replacement — the overlay layered on the derived builder satisfies that ordering.
///
/// The <c>auth</c> policy partitions by authenticated user id, falling back to remote IP for anonymous
/// callers. Unauthenticated <c>/token/issue</c> calls from the in-memory TestServer all share the same
/// (anonymous → IP) partition, so a burst past the permit limit trips the limiter deterministically.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class RateLimitingMiddlewareTests
{
    private const int AuthPermitLimit = 3;
    private readonly FshWebApplicationFactory _factory;

    public RateLimitingMiddlewareTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static readonly Dictionary<string, string?> RateLimitOverrides = new()
    {
        ["RateLimitingOptions:Enabled"] = "true",
        // Tiny auth window so a short burst trips the limiter; long window so it does not roll over mid-test.
        ["RateLimitingOptions:Auth:PermitLimit"] = AuthPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["RateLimitingOptions:Auth:WindowSeconds"] = "300",
        ["RateLimitingOptions:Auth:QueueLimit"] = "0",
        // Keep the global (tenant/user/ip) chained limiter generous so it never fires before the
        // auth-policy limiter we are actually testing.
        ["RateLimitingOptions:Ip:PermitLimit"] = "100000",
        ["RateLimitingOptions:User:PermitLimit"] = "100000",
        ["RateLimitingOptions:Tenant:PermitLimit"] = "100000",
    };

    private WebApplicationFactory<Program> CreateFactoryWithRateLimitingEnabled() =>
        _factory.WithWebHostBuilder(builder =>
        {
            // The rate limiter is read at TWO different times and the override must win at BOTH:
            //
            //   * AddHeroRateLimiting reads RateLimitingOptions EAGERLY (configuration.Get<>) at
            //     service-registration time, INSIDE AddHeroPlatform — i.e. before the host is built.
            //     A WithWebHostBuilder ConfigureAppConfiguration overlay is layered too late to be
            //     seen here, so the "auth" POLICY would not be registered. UseSetting writes into the
            //     web host builder's configuration which WebApplication.CreateBuilder surfaces EARLY,
            //     so it IS visible to this eager read and the auth policy gets wired.
            //
            //   * UseHeroRateLimiting (pipeline) and the BindConfiguration-backed IOptions read the
            //     value LATE from the app's IConfiguration. UseSetting values do NOT flow into that
            //     binding, so without an app-config overlay UseHeroRateLimiting would see Enabled=false
            //     and never install app.UseRateLimiter(). The ConfigureAppConfiguration overlay covers
            //     this read.
            //
            // Setting BOTH makes every read agree on Enabled=true plus the tiny auth limits.
            foreach (var kvp in RateLimitOverrides)
            {
                builder.UseSetting(kvp.Key, kvp.Value);
            }

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(RateLimitOverrides);
            });
        });

    #region Happy Path

    [Fact]
    public async Task TokenIssue_Should_Return429_When_AuthPolicyLimitExceeded()
    {
        // Arrange
        await using var factory = CreateFactoryWithRateLimitingEnabled();
        using var client = factory.CreateClient();

        // Bad credentials are fine — the auth rate-limit partition is evaluated before the
        // handler runs, so rejected logins still count against the limit.
        var responses = new List<HttpResponseMessage>();

        // Act — fire more requests than the permit limit allows into the same partition.
        try
        {
            for (int i = 0; i < AuthPermitLimit + 4; i++)
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
                request.Headers.Add("tenant", TestConstants.RootTenantId);
                request.Content = JsonContent.Create(new { email = "nobody@root.com", password = "wrong-password" });

                responses.Add(await client.SendAsync(request));
            }

            // Assert — at least one request past the limit must be rejected with 429.
            var tooMany = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests).ToList();
            tooMany.ShouldNotBeEmpty(
                $"Bursting past the auth permit limit must trip the production rate limiter (429). " +
                $"statuses={string.Join(",", responses.Select(r => (int)r.StatusCode))}");

            // The first permit-limit requests are allowed through (here they fail auth → 401),
            // proving the limiter is not blanket-rejecting.
            responses.Take(AuthPermitLimit).Any(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .ShouldBeFalse("Requests within the permit limit must not be rate limited.");

            var rejected = tooMany[0];

            // OnRejected sets Retry-After from the fixed-window lease metadata.
            rejected.Headers.Contains("Retry-After").ShouldBeTrue(
                "The rate limiter must set Retry-After on a 429.");

            // OnRejected writes an RFC 9457 ProblemDetails body (Type points at RFC 6585 §4).
            var body = await rejected.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;
            root.GetProperty("status").GetInt32().ShouldBe(429);
            root.GetProperty("title").GetString().ShouldBe("Too Many Requests");
            root.GetProperty("type").GetString()!.ShouldContain("rfc6585");
            root.TryGetProperty("traceId", out _).ShouldBeTrue();
        }
        finally
        {
            foreach (var r in responses)
            {
                r.Dispose();
            }
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task TokenIssue_Should_NotRateLimit_When_DisabledByDefault()
    {
        // Arrange — base factory keeps rate limiting OFF; a burst must never produce a 429.
        using var client = _factory.CreateClient();
        var statuses = new List<HttpStatusCode>();

        // Act
        for (int i = 0; i < AuthPermitLimit + 5; i++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
            request.Headers.Add("tenant", TestConstants.RootTenantId);
            request.Content = JsonContent.Create(new { email = "nobody@root.com", password = "wrong-password" });

            using var response = await client.SendAsync(request);
            statuses.Add(response.StatusCode);
        }

        // Assert
        statuses.ShouldNotContain(HttpStatusCode.TooManyRequests,
            "With RateLimitingOptions:Enabled=false the limiter must not be installed.");
    }

    #endregion
}
