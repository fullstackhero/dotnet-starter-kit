using FSH.Modules.Auditing;
using FSH.Modules.Auditing.Contracts;
using Microsoft.AspNetCore.Http;

namespace Auditing.Tests.Http;

/// <summary>
/// Regression guard: <see cref="AuditHttpMiddleware"/> buffers the response body into a MemoryStream
/// and only copies it to the socket after the handler returns. A long-lived streaming handler (SSE)
/// never returns, so buffering it left the client hanging forever on "connecting" with zero bytes.
/// Streaming requests (identified by the <c>text/event-stream</c> Accept header) must pass straight
/// through with the original response body intact.
/// </summary>
public sealed class AuditHttpMiddlewareStreamingTests
{
    private sealed class StubAuditPublisher : IAuditPublisher
    {
        public IAuditScope CurrentScope => throw new NotSupportedException("not used on the streaming path");
        public ValueTask PublishAsync(IAuditEvent auditEvent, CancellationToken ct = default) => ValueTask.CompletedTask;
    }

    [Theory]
    [InlineData("text/event-stream")]
    [InlineData("text/event-stream, */*")]
    [InlineData("application/json, text/event-stream")]
    public async Task InvokeAsync_Should_NotBufferResponseBody_For_EventStreamRequests(string accept)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Accept = accept;
        using var originalBody = new MemoryStream();
        ctx.Response.Body = originalBody;

        Stream? bodySeenByHandler = null;
        Task Next(HttpContext c)
        {
            bodySeenByHandler = c.Response.Body;
            return Task.CompletedTask;
        }

        var middleware = new AuditHttpMiddleware(Next, new AuditHttpOptions(), new StubAuditPublisher());

        await middleware.InvokeAsync(ctx);

        // The handler must see the real response body, not a swapped-in audit buffer.
        bodySeenByHandler.ShouldBeSameAs(originalBody);
        ctx.Response.Body.ShouldBeSameAs(originalBody);
    }
}
