using System.Text.Json;
using FSH.Framework.Web.Exceptions;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Primitives;

namespace Integration.Tests.Tests.Middleware;

/// <summary>
/// Exercises the PRODUCTION <c>GlobalExceptionHandler</c>. The default test host swaps it out for
/// <c>DetailedTestExceptionHandler</c> (which leaks raw exception messages) inside
/// <c>FshWebApplicationFactory.ConfigureWebHost</c>.
///
/// Re-wiring technique (a derived factory via
/// <see cref="WebApplicationFactory{TEntryPoint}.WithWebHostBuilder"/> + <c>ConfigureTestServices</c>,
/// which runs AFTER the base factory's <c>ConfigureServices</c> so it gets the final word):
///
///   1. Remove every registered <see cref="IExceptionHandler"/> (the test double) and re-register the
///      real <see cref="GlobalExceptionHandler"/>. <c>app.UseExceptionHandler()</c> (already wired by
///      <c>UseHeroPlatform</c>) resolves <c>IExceptionHandler</c> from DI, so swapping the DI
///      registration is sufficient to drive the production handler — no pipeline edits required.
///
///   2. Add a test-only endpoint that throws a RAW (un-mapped) exception. Real endpoints only throw
///      framework exceptions (CustomException / NotFound / ...) which map to 4xx; to hit the generic
///      500 branch we need a raw throw at the endpoint terminal, downstream of <c>UseExceptionHandler</c>.
///      A DI-only <c>EndpointDataSource</c> is NOT routable here (minimal hosting routes against the
///      composite built from the app's own <c>DataSources</c>, not the DI container), so instead an
///      <see cref="IStartupFilter"/> grabs the live <see cref="IEndpointRouteBuilder"/> from
///      <c>app.Properties</c> after the inner pipeline has configured routing and appends the throwing
///      endpoint to its (live) <c>DataSources</c> collection.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class GlobalExceptionHandlerTests
{
    private const string ThrowRoute = "/__throw-raw-test";
    private readonly FshWebApplicationFactory _factory;

    public GlobalExceptionHandlerTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateFactoryWithRealExceptionHandler() =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // 1. Restore the production exception handler in place of the test double.
                var existing = services
                    .Where(d => d.ServiceType == typeof(IExceptionHandler))
                    .ToList();
                foreach (var d in existing)
                {
                    services.Remove(d);
                }
                services.AddExceptionHandler<GlobalExceptionHandler>();

                // 2. Append a test-only throwing endpoint into the live route builder after routing is set up.
                services.TryAddEnumerable(
                    ServiceDescriptor.Transient<IStartupFilter, ThrowingEndpointStartupFilter>());
            });
        });

    #region Exception

    [Fact]
    public async Task UnhandledException_Should_ReturnProductionProblemDetails_When_GlobalHandlerWired()
    {
        // Arrange
        await using var factory = CreateFactoryWithRealExceptionHandler();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(ThrowRoute);
        var body = await response.Content.ReadAsStringAsync();

        // Assert — production RFC 9457 ProblemDetails for an unhandled exception.
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.GetProperty("status").GetInt32().ShouldBe(500);
        root.GetProperty("title").GetString().ShouldBe("An unexpected error occurred");

        // The production handler returns a GENERIC detail — never the raw exception message.
        // This is the behavior the test double deliberately bypasses.
        var detail = root.GetProperty("detail").GetString();
        detail.ShouldBe("An unexpected error occurred. Please try again later.");
        detail!.ShouldNotContain("ThrowingEndpoint");

        // traceId + correlationId extensions are always surfaced for support correlation.
        root.TryGetProperty("traceId", out var traceId).ShouldBeTrue();
        traceId.GetString().ShouldNotBeNullOrWhiteSpace();
        root.TryGetProperty("correlationId", out _).ShouldBeTrue();

        // instance echoes the request path.
        root.GetProperty("instance").GetString().ShouldBe(ThrowRoute);
    }

    [Fact]
    public async Task UnhandledException_Should_ReturnJsonBody_When_GlobalHandlerWired()
    {
        // Arrange
        await using var factory = CreateFactoryWithRealExceptionHandler();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(ThrowRoute);

        // Assert — GlobalExceptionHandler serializes the ProblemDetails via WriteAsJsonAsync,
        // which emits application/json (it does not set the problem+json media type itself).
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    #endregion

    /// <summary>
    /// After the application's own pipeline configuration runs (which calls <c>UseRouting</c> and stashes
    /// the <see cref="IEndpointRouteBuilder"/> in <c>app.Properties</c>), appends a single GET endpoint
    /// that throws a raw exception. The route builder's data source list is live, so the late addition is
    /// matched at request time and the throw propagates up to the production exception handler.
    /// </summary>
    private sealed class ThrowingEndpointStartupFilter : IStartupFilter
    {
        private const string EndpointRouteBuilderKey = "__EndpointRouteBuilder";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                next(app);

                if (app.Properties.TryGetValue(EndpointRouteBuilderKey, out var value) &&
                    value is IEndpointRouteBuilder endpoints)
                {
                    endpoints.DataSources.Add(new ThrowingEndpointDataSource(ThrowRoute));
                }
            };
    }

    /// <summary>
    /// A minimal <see cref="EndpointDataSource"/> exposing a single anonymous GET endpoint that throws a
    /// raw (un-mapped) exception, so it falls through to the GlobalExceptionHandler's generic 500 branch.
    /// </summary>
    private sealed class ThrowingEndpointDataSource : EndpointDataSource
    {
        private readonly IReadOnlyList<Endpoint> _endpoints;

        public ThrowingEndpointDataSource(string route)
        {
            var builder = new RouteEndpointBuilder(
                requestDelegate: _ => throw new InvalidOperationException("Boom from ThrowingEndpoint"),
                routePattern: RoutePatternFactory.Parse(route),
                order: 0)
            {
                DisplayName = "TEST /__throw-raw-test",
            };
            builder.Metadata.Add(new HttpMethodMetadata(["GET"]));
            builder.Metadata.Add(new AllowAnonymousAttribute());

            _endpoints = [builder.Build()];
        }

        public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

        // The endpoint set never changes, so hand back a token that never fires.
        public override IChangeToken GetChangeToken() =>
            new CancellationChangeToken(CancellationToken.None);
    }
}
