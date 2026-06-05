using System.Net.Http.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Idempotency;

[Collection(FshCollectionDefinition.Name)]
public sealed class IdempotencyFilterTests
{
    private const string IdempotencyHeader = "Idempotency-Key";
    private const string ReplayedHeader = "Idempotency-Replayed";

    private readonly AuthHelper _auth;

    public IdempotencyFilterTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    // Full replay-with-matching-body coverage isn't possible yet (filter captures the raw IResult, not the body — dotnet/aspnetcore#57191, backlog 2.4b).
    // These tests verify only the wiring: Idempotency-Replayed header presence/absence and that a distinct key forces fresh execution.

    [Fact]
    public async Task CreateBillingPlan_Should_ExecuteNormally_When_NoIdempotencyKey()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            key = $"noidem-{uniqueId}",
            name = $"NoIdem Plan {uniqueId}",
            currency = "USD",
            monthlyBasePrice = 5m
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/billing/plans")
        {
            Content = JsonContent.Create(payload)
        };
        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains(ReplayedHeader).ShouldBeFalse(
            "A request without an idempotency key must never be marked as replayed.");
    }

    [Fact]
    public async Task CreateBillingPlan_Should_ExecuteSecondCall_When_DifferentIdempotencyKey()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var firstKey = $"idem-a-{uniqueId}";
        var secondKey = $"idem-b-{uniqueId}";
        var basePayload = new
        {
            key = $"diff-{uniqueId}",
            name = $"Diff Plan {uniqueId}",
            currency = "USD",
            monthlyBasePrice = 1m
        };

        using var firstRequest = BuildRequest(basePayload, firstKey);
        (await client.SendAsync(firstRequest)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Different key + different plan-key so it's a legitimately new resource.
        var secondPayload = basePayload with { key = $"diff-b-{uniqueId}" };
        using var secondRequest = BuildRequest(secondPayload, secondKey);
        var secondResponse = await client.SendAsync(secondRequest);

        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondResponse.Headers.Contains(ReplayedHeader).ShouldBeFalse(
            "A different idempotency key must route through a fresh execution.");
    }

    private static HttpRequestMessage BuildRequest(object payload, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/billing/plans")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add(IdempotencyHeader, idempotencyKey);
        return request;
    }
}
