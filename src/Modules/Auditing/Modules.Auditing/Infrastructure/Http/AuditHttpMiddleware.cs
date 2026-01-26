using FSH.Modules.Auditing.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace FSH.Modules.Auditing;

public sealed class AuditHttpMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuditHttpOptions _opts;
    private readonly IAuditPublisher _publisher;

    public AuditHttpMiddleware(RequestDelegate next, AuditHttpOptions opts, IAuditPublisher publisher)
        => (_next, _opts, _publisher) = (next, opts, publisher);

    public async Task InvokeAsync(HttpContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        if (ShouldSkip(ctx))
        {
            await _next(ctx);
            return;
        }

        var requestContext = await CaptureRequestAsync(ctx);
        var sw = Stopwatch.StartNew();

        var originalBody = ctx.Response.Body;
        await using var responseBuffer = new MemoryStream();
        ctx.Response.Body = responseBuffer;

        try
        {
            await _next(ctx);
            sw.Stop();

            await WriteSuccessAuditAsync(ctx, requestContext, responseBuffer, originalBody, sw);
        }
        catch (Exception ex)
        {
            sw.Stop();
            await WriteExceptionAuditAsync(ctx, ex);
            ctx.Response.Body = originalBody;
            throw;
        }
    }

    private async Task<RequestCaptureContext> CaptureRequestAsync(HttpContext ctx)
    {
        object? reqPreview = null;
        int reqSize = 0;

        if (ShouldCaptureBody(ctx.Request.ContentType))
        {
            var masker = ctx.RequestServices.GetService<IAuditMaskingService>();
            (reqPreview, reqSize) = await HttpBodyReader.ReadRequestAsync(ctx, _opts.MaxRequestBytes, ctx.RequestAborted);

            if (reqPreview is not null && masker is not null)
            {
                reqPreview = masker.ApplyMasking(reqPreview);
            }
        }

        return new RequestCaptureContext(reqPreview, reqSize);
    }

    private async Task WriteSuccessAuditAsync(
        HttpContext ctx,
        RequestCaptureContext requestContext,
        MemoryStream responseBuffer,
        Stream originalBody,
        Stopwatch sw)
    {
        var (respPreview, respSize) = await CaptureResponseAsync(ctx, responseBuffer);

        await RestoreResponseBodyAsync(responseBuffer, originalBody, ctx);

        await WriteActivityAuditAsync(ctx, requestContext, respPreview, respSize, sw);
    }

    private async Task<(object? Preview, int Size)> CaptureResponseAsync(HttpContext ctx, MemoryStream responseBuffer)
    {
        if (!ShouldCaptureBody(ctx.Response.ContentType))
        {
            return (null, 0);
        }

        var masker = ctx.RequestServices.GetService<IAuditMaskingService>();
        responseBuffer.Position = 0;

        await using var respBuffer = new MemoryStream();
        await responseBuffer.CopyToAsync(respBuffer, ctx.RequestAborted);

        var (respPreview, respSize) = await HttpBodyReader.ReadResponseAsync(
            respBuffer, _opts.MaxResponseBytes, ctx.RequestAborted);

        if (respPreview is not null && masker is not null)
        {
            respPreview = masker.ApplyMasking(respPreview);
        }

        return (respPreview, respSize);
    }

    private static async Task RestoreResponseBodyAsync(MemoryStream responseBuffer, Stream originalBody, HttpContext ctx)
    {
        responseBuffer.Position = 0;
        ctx.Response.Body = originalBody;

        if (responseBuffer.Length > 0)
        {
            await responseBuffer.CopyToAsync(originalBody, ctx.RequestAborted);
        }
    }

    private async Task WriteActivityAuditAsync(
        HttpContext ctx,
        RequestCaptureContext requestContext,
        object? respPreview,
        int respSize,
        Stopwatch sw)
    {
        await Audit.ForActivity(Contracts.ActivityKind.Http, ctx.Request.Path)
            .WithActivityResult(
                statusCode: ctx.Response.StatusCode,
                durationMs: (int)sw.Elapsed.TotalMilliseconds,
                captured: _opts.CaptureBodies ? BodyCapture.Both : BodyCapture.None,
                requestSize: requestContext.Size,
                responseSize: respSize,
                requestPreview: requestContext.Preview,
                responsePreview: respPreview)
            .WithSource("api")
            .WithTenant(_publisher.CurrentScope?.TenantId)
            .WithUser(_publisher.CurrentScope?.UserId, _publisher.CurrentScope?.UserName)
            .WithCorrelation(_publisher.CurrentScope?.CorrelationId ?? ctx.TraceIdentifier)
            .WithRequestId(_publisher.CurrentScope?.RequestId ?? ctx.TraceIdentifier)
            .WriteAsync(ctx.RequestAborted);
    }

    private async Task WriteExceptionAuditAsync(HttpContext ctx, Exception ex)
    {
        var sev = ExceptionSeverityClassifier.Classify(ex);
        if (sev < _opts.MinExceptionSeverity)
        {
            return;
        }

        await Audit.ForException(ex, ExceptionArea.Api, routeOrLocation: ctx.Request.Path, severity: sev)
            .WithSource("api")
            .WithTenant(_publisher.CurrentScope?.TenantId)
            .WithUser(_publisher.CurrentScope?.UserId, _publisher.CurrentScope?.UserName)
            .WithCorrelation(_publisher.CurrentScope?.CorrelationId ?? ctx.TraceIdentifier)
            .WithRequestId(_publisher.CurrentScope?.RequestId ?? ctx.TraceIdentifier)
            .WriteAsync(ctx.RequestAborted);
    }

    private bool ShouldCaptureBody(string? contentType) =>
        _opts.CaptureBodies && ContentTypeHelper.IsJsonLike(contentType, _opts.AllowedContentTypes);

    private bool ShouldSkip(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? string.Empty;
        return _opts.ExcludePathStartsWith.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private readonly record struct RequestCaptureContext(object? Preview, int Size);
}
