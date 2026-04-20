using FSH.Framework.Core.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Web.Sse;

public static class SseEndpoints
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Maps the SSE token exchange endpoint (<c>POST /api/v1/sse/token</c>, authenticated via JWT) and
    /// the streaming endpoint (<c>GET /api/v1/sse/stream?token=&lt;guid&gt;</c>). Browsers' EventSource
    /// API cannot send an Authorization header, so the stream authenticates via a short-lived opaque
    /// token issued from the token endpoint.
    /// </summary>
    public static IEndpointRouteBuilder MapHeroSseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/sse/token", async (
            ICurrentUser currentUser,
            ISseTokenService tokens,
            CancellationToken cancellationToken) =>
        {
            if (!currentUser.IsAuthenticated())
            {
                return Results.Unauthorized();
            }

            var userId = currentUser.GetUserId().ToString();
            var tenantId = currentUser.GetTenant();
            var token = await tokens.IssueAsync(userId, tenantId, cancellationToken).ConfigureAwait(false);
            return Results.Ok(new { token });
        })
        .WithName("SseToken")
        .WithSummary("Issue a short-lived SSE stream token")
        .WithTags("SSE")
        .RequireAuthorization();

        endpoints.MapGet("/api/v1/sse/stream", async (
            HttpContext context,
            [Microsoft.AspNetCore.Mvc.FromQuery] Guid token,
            ISseTokenService tokens,
            SseConnectionManager connectionManager,
            CancellationToken cancellationToken) =>
        {
            var principal = await tokens.ConsumeAsync(token, cancellationToken).ConfigureAwait(false);
            if (principal is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.Headers["X-Accel-Buffering"] = "no"; // disable nginx buffering

            var (connectionId, reader) = connectionManager.Connect(principal.UserId, principal.TenantId);
            using var heartbeat = new PeriodicTimer(HeartbeatInterval);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var waitTask = reader.WaitToReadAsync(cancellationToken).AsTask();
                    var tickTask = heartbeat.WaitForNextTickAsync(cancellationToken).AsTask();

                    var completed = await Task.WhenAny(waitTask, tickTask).ConfigureAwait(false);

                    if (completed == tickTask)
                    {
                        _ = await tickTask.ConfigureAwait(false);
                        await context.Response.WriteAsync(":heartbeat\n\n", cancellationToken).ConfigureAwait(false);
                        await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    var hasData = await waitTask.ConfigureAwait(false);
                    if (!hasData)
                    {
                        break;
                    }

                    while (reader.TryRead(out var sseEvent))
                    {
                        await WriteEventAsync(context.Response, sseEvent, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Client disconnected — expected.
            }
            finally
            {
                connectionManager.Disconnect(connectionId);
            }
        })
        .WithName("SseStream")
        .WithSummary("Server-Sent Events stream (authenticates via ?token= issued from /sse/token)")
        .WithTags("SSE")
        .AllowAnonymous()
        .ExcludeFromDescription();

        return endpoints;
    }

    private static async Task WriteEventAsync(HttpResponse response, SseEvent sseEvent, CancellationToken ct)
    {
        if (sseEvent.Id is not null)
        {
            await response.WriteAsync($"id: {sseEvent.Id}\n", ct).ConfigureAwait(false);
        }

        await response.WriteAsync($"event: {sseEvent.EventType}\n", ct).ConfigureAwait(false);

        // SSE spec: multi-line data needs each line prefixed with "data: "
        foreach (var line in sseEvent.Data.Split('\n'))
        {
            await response.WriteAsync($"data: {line}\n", ct).ConfigureAwait(false);
        }

        await response.WriteAsync("\n", ct).ConfigureAwait(false);
        await response.Body.FlushAsync(ct).ConfigureAwait(false);
    }
}
