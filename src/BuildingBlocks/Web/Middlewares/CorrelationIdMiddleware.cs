using FSH.Framework.Core.Context;
using Microsoft.AspNetCore.Http;

namespace FSH.Framework.Web.Middlewares;

public class CorrelationIdMiddleware(ICorrelationIdInitializer correlationIdInitializer) : IMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly ICorrelationIdInitializer _correlationIdInitializer = correlationIdInitializer;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            correlationId = context.TraceIdentifier;
        }

        _correlationIdInitializer.SetCorrelationId(correlationId!);
        context.TraceIdentifier = correlationId!;

        // Also add to response headers for predictability
        if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
        {
            context.Response.Headers.Append(CorrelationIdHeader, correlationId);
        }

        await next(context);
    }
}
