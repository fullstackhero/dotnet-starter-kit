using FSH.Framework.Web.Idempotency;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Integration.Tests.Tests.Idempotency;

/// <summary>
/// The filter detaches the client's abort token so an idempotent handler always reaches the end and
/// leaves a response to store. Minimal-API parameter binding, however, resolves the handler's
/// <see cref="CancellationToken"/> from <c>HttpContext.RequestAborted</c> BEFORE endpoint filters
/// run, so the token the handler actually receives is the one captured at binding time. These run a
/// real host over the real filter: a substitute filter or a hand-built
/// <c>EndpointFilterInvocationContext</c> skips binding entirely and cannot see the difference.
/// </summary>
public sealed class IdempotencyCancellationBindingTests
{
    private const string IdempotencyHeader = "Idempotency-Key";

    [Fact]
    public async Task IdempotentHandler_Should_ReceiveNonCancellableToken_When_FilterDetachesClientAbort()
    {
        // Arrange
        CancellationToken observed = default;
        using var host = await StartHostAsync(ct =>
        {
            observed = ct;
            return Task.CompletedTask;
        });

        // Act
        using var response = await SendAsync(host, Guid.NewGuid().ToString("N"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        observed.CanBeCanceled.ShouldBeFalse(
            "The handler's bound CancellationToken must be detached too; reassigning HttpContext." +
            "RequestAborted alone leaves the already-bound token wired to the client's abort.");
    }

    [Fact]
    public async Task IdempotentHandler_Should_RunToCompletion_When_ClientAbortIsSignalled()
    {
        // Arrange: the handler awaits its own token after the client has gone away. Attached, the
        // await throws and the filter stores nothing, which is the duplicate this filter exists to
        // stop.
        var reachedEnd = false;
        using var host = await StartHostAsync(async ct =>
        {
            await Task.Delay(20, ct);
            reachedEnd = true;
        });

        // Act
        using var response = await SendAsync(host, Guid.NewGuid().ToString("N"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        reachedEnd.ShouldBeTrue("A detached handler must reach its end so the filter has a response to store.");
    }

    private static async Task<IHost> StartHostAsync(Func<CancellationToken, Task> handler)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        // The filter resolves these from the request scope; the optional Redis multiplexer and the
        // tenant accessor stay absent on purpose, which is the single-instance path.
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddOptions<IdempotencyOptions>();

        var app = builder.Build();
        app.MapPost("/probe", async (CancellationToken ct) =>
        {
            await handler(ct);
            return TypedResults.Ok(new { ok = true });
        }).WithIdempotency();

        await app.StartAsync();
        return app;
    }

    private static async Task<HttpResponseMessage> SendAsync(IHost host, string idempotencyKey)
    {
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/probe");
        request.Headers.Add(IdempotencyHeader, idempotencyKey);
        return await client.SendAsync(request);
    }
}
