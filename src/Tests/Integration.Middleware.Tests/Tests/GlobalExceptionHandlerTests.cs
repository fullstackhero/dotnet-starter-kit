using System.Text.Json;
using Integration.Middleware.Tests.Infrastructure;

namespace Integration.Middleware.Tests.Tests;

/// <summary>
/// Exercises the production <see cref="FSH.Framework.Web.Exceptions.GlobalExceptionHandler"/>
/// end-to-end by hitting an endpoint that throws a raw exception. Because this assembly does NOT
/// swap in a test exception handler, the response is the real RFC 9457 ProblemDetails the API
/// would return in production.
/// </summary>
[Collection(MiddlewareCollectionDefinition.Name)]
public sealed class GlobalExceptionHandlerTests
{
    private readonly MiddlewareWebApplicationFactory _factory;

    public GlobalExceptionHandlerTests(MiddlewareWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region Exception

    [Fact]
    public async Task ThrowEndpoint_Should_Return500ProblemDetails_When_UnhandledExceptionIsThrown()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/__test/throw");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("status", out var status).ShouldBeTrue();
        status.GetInt32().ShouldBe(500);

        root.TryGetProperty("title", out var title).ShouldBeTrue();
        title.GetString().ShouldNotBeNullOrWhiteSpace();

        // GlobalExceptionHandler surfaces a traceId extension for client/support correlation.
        root.TryGetProperty("traceId", out var traceId).ShouldBeTrue();
        traceId.GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ThrowEndpoint_Should_ReturnJsonContentType_When_HandledByGlobalExceptionHandler()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/__test/throw");

        // Assert
        // GlobalExceptionHandler writes via WriteAsJsonAsync, which emits "application/json"
        // (NOT "application/problem+json" — only the rate-limiter rejection path sets that).
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    #endregion
}
