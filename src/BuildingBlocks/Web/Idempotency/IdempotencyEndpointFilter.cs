using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FSH.Framework.Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Web.Idempotency;

/// <summary>
/// Endpoint filter that provides idempotency for POST/PUT/PATCH requests.
/// When an Idempotency-Key header is present, the response is cached and replayed
/// for subsequent requests with the same key.
/// </summary>
public sealed class IdempotencyEndpointFilter : IEndpointFilter
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly HybridCacheEntryOptions ProbeOnlyOptions = new() { Flags = HybridCacheEntryFlags.DisableUnderlyingData };

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var httpContext = context.HttpContext;
        var options = httpContext.RequestServices.GetRequiredService<IOptions<IdempotencyOptions>>().Value;
        var idempotencyKey = httpContext.Request.Headers[options.HeaderName].ToString();

        // No header = pass through (idempotency is opt-in per request)
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return await next(context).ConfigureAwait(false);
        }

        if (idempotencyKey.Length > options.MaxKeyLength)
        {
            return TypedResults.BadRequest($"Idempotency key exceeds maximum length of {options.MaxKeyLength}.");
        }

        var cache = httpContext.RequestServices.GetRequiredService<HybridCache>();
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<IdempotencyEndpointFilter>>();

        // Include tenant context in cache key for isolation
        var tenantId = httpContext.User.FindFirst("tenant")?.Value ?? "global";
        var cacheKey = CacheKeys.IdempotencyEntry(tenantId, idempotencyKey);
        var tags = new[] { CacheKeys.Tags.Idempotency, CacheKeys.Tags.Tenant(tenantId) };

        // Probe-only read: DisableUnderlyingData prevents the factory from running so a miss
        // returns null without caching or executing the handler here. The factory lambda is
        // mandatory per API contract but will never be invoked with this flag.
        var cached = await cache.GetOrCreateAsync<CachedIdempotentResponse?>(
            cacheKey,
            static _ => ValueTask.FromResult<CachedIdempotentResponse?>(null),
            options: ProbeOnlyOptions,
            cancellationToken: httpContext.RequestAborted).ConfigureAwait(false);
        if (cached is not null)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Idempotent replay for key {KeyHash}", HashKey(idempotencyKey));
            }
            httpContext.Response.Headers["Idempotency-Replayed"] = "true";
            httpContext.Response.StatusCode = cached.StatusCode;
            if (cached.ContentType is not null)
            {
                httpContext.Response.ContentType = cached.ContentType;
            }

            if (cached.Body.Length > 0)
            {
                await httpContext.Response.Body.WriteAsync(cached.Body, httpContext.RequestAborted).ConfigureAwait(false);
            }

            return null; // Response already written
        }

        // Execute the handler
        var result = await next(context).ConfigureAwait(false);

        // Cache the response
        try
        {
            var body = result is not null ? JsonSerializer.SerializeToUtf8Bytes(result, JsonOpts) : [];
            var responseToCache = new CachedIdempotentResponse
            {
                StatusCode = httpContext.Response.StatusCode is > 0 and < 600 ? httpContext.Response.StatusCode : 200,
                ContentType = "application/json",
                Body = body
            };

            var setOptions = new HybridCacheEntryOptions
            {
                Expiration = options.DefaultTtl,
                LocalCacheExpiration = options.DefaultTtl < TimeSpan.FromMinutes(2) ? options.DefaultTtl : TimeSpan.FromMinutes(2),
            };
            await cache.SetAsync(cacheKey, responseToCache, setOptions, tags, httpContext.RequestAborted).ConfigureAwait(false);
        }
        // Best-effort caching: idempotency replay is a convenience, not a correctness requirement
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to cache idempotent response for key {KeyHash}", HashKey(idempotencyKey));
        }

        return result;
    }

    private static string HashKey(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}

public static class IdempotencyEndpointExtensions
{
    /// <summary>
    /// Enables idempotency for this endpoint. Requires Idempotency-Key header on requests.
    /// Duplicate requests with the same key return the cached response.
    /// </summary>
    public static RouteHandlerBuilder WithIdempotency(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEndpointFilter<IdempotencyEndpointFilter>();
    }
}
