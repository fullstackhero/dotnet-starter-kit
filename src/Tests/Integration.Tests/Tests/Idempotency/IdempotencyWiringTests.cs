using FSH.Framework.Web.Idempotency;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Idempotency;

[Collection(FshCollectionDefinition.Name)]
public sealed class IdempotencyWiringTests
{
    private readonly FshWebApplicationFactory _factory;

    public IdempotencyWiringTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // The entry key scopes by caller, and ResolveCaller returns "anon" for every unauthenticated request.
    // So on an anonymous endpoint all callers share one bucket: two people sending the same low-entropy
    // key ("1", "retry") against one tenant collide, and the second replays the first's response instead
    // of being served. This asserts the shape rather than the single endpoint that had the problem, so
    // re-adding .WithIdempotency() to an anonymous endpoint fails here instead of in production.
    [Fact]
    public void AnonymousEndpoints_Should_NotBeIdempotent()
    {
        _ = _factory.Server;

        var idempotentAnonymous = IdempotentEndpoints()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => endpoint.RoutePattern.RawText ?? endpoint.DisplayName ?? "<unnamed>")
            .ToList();

        idempotentAnonymous.ShouldBeEmpty(
            "Anonymous endpoints share the \"anon\" caller bucket, so an idempotency key from one caller " +
            "can replay another caller's response:\n  - " + string.Join("\n  - ", idempotentAnonymous));
    }

    // Guards the test above against passing vacuously: if WithIdempotency() ever stops attaching the
    // marker, the query above returns nothing and would report success over an unchecked endpoint map.
    [Fact]
    public void IdempotentEndpoints_Should_BeDiscoverableFromTheEndpointMap()
    {
        _ = _factory.Server;

        IdempotentEndpoints().ShouldNotBeEmpty(
            "No endpoint carries IdempotentEndpointMetadata, so nothing can assert idempotency wiring.");
    }

    private List<RouteEndpoint> IdempotentEndpoints() =>
        _factory.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints.OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IdempotentEndpointMetadata>() is not null)
            .ToList();
}
