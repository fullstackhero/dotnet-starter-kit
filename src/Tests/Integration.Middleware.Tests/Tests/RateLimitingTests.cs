using System.Net;
using System.Text.Json;
using Integration.Middleware.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Integration.Middleware.Tests.Tests;

/// <summary>
/// Exercises the production "auth" rate-limit policy attached to the token-issue endpoint.
/// The factory configures Auth:PermitLimit=3 with a 300s window so a small burst of bad-credential
/// requests deterministically trips the limiter and produces the production OnRejected response
/// (429 + Retry-After + RFC 9457 ProblemDetails).
/// </summary>
[Collection(MiddlewareCollectionDefinition.Name)]
public sealed class RateLimitingTests
{
    private const int AuthPermitLimit = 3;

    private readonly MiddlewareWebApplicationFactory _factory;

    public RateLimitingTests(MiddlewareWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> IssueBadTokenFromAsync(IPAddress clientIp)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(new { email = "nobody@example.com", password = "wrong-password" });
        var context = await _factory.Server.SendAsync(c =>
        {
            c.Connection.RemoteIpAddress = clientIp;
            c.Request.Method = HttpMethods.Post;
            c.Request.Path = $"{TestConstants.IdentityBasePath}/token/issue";
            c.Request.Headers["tenant"] = TestConstants.RootTenantId;
            c.Request.ContentType = "application/json";
            c.Request.ContentLength = body.Length;
            c.Request.Body = new MemoryStream(body);
        });
        return context.Response.StatusCode;
    }

    private static async Task<HttpResponseMessage> IssueBadTokenAsync(HttpClient client)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
        request.Headers.Add("tenant", TestConstants.RootTenantId);
        request.Content = JsonContent.Create(new { email = "nobody@example.com", password = "wrong-password" });
        return await client.SendAsync(request);
    }

    #region Happy Path

    [Fact]
    public async Task TokenIssue_Should_NotRateLimit_When_RequestsAreWithinTheAuthLimit()
    {
        // Arrange: a client IP of its own. TestServer leaves RemoteIpAddress null, so requests from
        // CreateClient() share the "unknown IP" partition with the 429 test below. If that test ran
        // first, this one would start on an exhausted window.
        var clientIp = IPAddress.Parse("203.0.113.10");

        // Act + Assert: the first PermitLimit requests must NOT be rejected with 429.
        for (int i = 0; i < AuthPermitLimit; i++)
        {
            var statusCode = await IssueBadTokenFromAsync(clientIp);
            statusCode.ShouldNotBe(
                (int)HttpStatusCode.TooManyRequests,
                $"request #{i + 1} within the auth limit should not be rate limited");
        }
    }

    #endregion

    #region Exception

    [Fact]
    public async Task TokenIssue_Should_Return429WithRetryAfterAndProblemDetails_When_AuthLimitIsExceeded()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act: burst well past the limit so at least one request is rejected.
        var responses = new List<HttpResponseMessage>();
        try
        {
            for (int i = 0; i < AuthPermitLimit + 5; i++)
            {
                responses.Add(await IssueBadTokenAsync(client));
            }

            var rejected = responses.FirstOrDefault(r => r.StatusCode == HttpStatusCode.TooManyRequests);

            // Assert
            rejected.ShouldNotBeNull("expected at least one 429 after bursting past the auth limit");

            rejected.Headers.TryGetValues("Retry-After", out var retryAfter).ShouldBeTrue();
            retryAfter.ShouldNotBeEmpty();

            var body = await rejected.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            root.TryGetProperty("status", out var status).ShouldBeTrue();
            status.GetInt32().ShouldBe(429);

            root.TryGetProperty("title", out var title).ShouldBeTrue();
            title.GetString().ShouldBe("Too Many Requests");
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
}
