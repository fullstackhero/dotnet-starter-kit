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
        using var host = await StartHostAsync(
            ct =>
            {
                observed = ct;
                return Task.CompletedTask;
            },
            clientAbort: CancellationToken.None);

        // Act
        using var response = await SendAsync(host, Guid.NewGuid().ToString("N"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        observed.CanBeCanceled.ShouldBeFalse(
            "The handler's bound CancellationToken must be detached too; reassigning HttpContext." +
            "RequestAborted alone leaves the already-bound token wired to the client's abort.");
    }

    [Fact]
    public async Task IdempotentHandler_Should_RunToCompletionAndStore_When_ClientAbortIsSignalled()
    {
        // Arrange: the client hangs up while the handler is running. The handler awaits its own
        // token afterwards, so with the token still attached it throws, the filter stores nothing,
        // and the client's retry re-executes a side effect that already committed. That retry is the
        // whole point, so it is what gets asserted here, not just that the handler reached its end.
        var executions = 0;
        using var clientGone = new CancellationTokenSource();
        using var host = await StartHostAsync(
            async ct =>
            {
                executions++;
                if (executions == 1)
                {
                    await clientGone.CancelAsync();
                }

                await Task.Delay(20, ct);
            },
            clientGone.Token);

        // Act: the first response never reaches the client, which is correct and not what is under
        // test. Asserted rather than swallowed: writing the body to a connection that is gone is the
        // failure the store is deliberately sequenced ahead of.
        var key = Guid.NewGuid().ToString("N");
        await Should.ThrowAsync<OperationCanceledException>(() => SendAsync(host, key));

        using var replay = await SendAsync(host, key);

        // Assert
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);
        executions.ShouldBe(
            1,
            "the handler ran to completion despite the abort, so its response was stored and the " +
            "retry replayed it instead of committing the side effect a second time.");
    }

    // clientAbort stands in for the connection dropping: a middleware ahead of the endpoint publishes
    // it as RequestAborted, which is what minimal-API binding hands the handler and what the filter
    // has to detach. TestServer has no way to hang up a live request from the client side.
    private static async Task<IHost> StartHostAsync(Func<CancellationToken, Task> handler, CancellationToken clientAbort)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        // The filter resolves these from the request scope; the optional Redis multiplexer and the
        // tenant accessor stay absent on purpose, which is the single-instance path.
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddOptions<IdempotencyOptions>();

        var app = builder.Build();
        if (clientAbort.CanBeCanceled)
        {
            // First request only: that is the one whose client hangs up. The retry arrives on a new
            // connection, so publishing the already-cancelled token to it too would fail the replay
            // for a reason that has nothing to do with what is being tested.
            var firstRequest = 1;
            app.Use(async (context, next) =>
            {
                if (Interlocked.Exchange(ref firstRequest, 0) == 1)
                {
                    context.RequestAborted = clientAbort;
                }

                await next(context);
            });
        }

        app.MapPost("/probe", async (CancellationToken ct) =>
        {
            await handler(ct);
            return TypedResults.Ok(new { ok = true });
        }).WithIdempotency();

        // CancellationToken.None: clientAbort stands for the client dropping mid-request, not for a
        // reason to abandon host startup.
        await app.StartAsync(CancellationToken.None);
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
