using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FSH.Framework.Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
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
/// <remarks>
/// Uses <see cref="IDistributedCache"/> directly for the probe read (bypassing
/// <see cref="ITenantCacheService"/>'s factory-mandatory API) and
/// <see cref="ITenantCacheService.SetAsync{T}"/> for the write path so replays
/// benefit from L1 and the regular tag invalidation story. Tenant scoping is
/// applied automatically by <see cref="ITenantCacheService"/> — no manual key
/// prefixing required in the write path.
/// Using <c>HybridCache</c> with <c>DisableUnderlyingData</c> as a "get-only probe" is a
/// known anti-pattern tracked at dotnet/aspnetcore#57191.
/// </remarks>
public sealed class IdempotencyEndpointFilter : IEndpointFilter
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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

        var distributedCache = httpContext.RequestServices.GetRequiredService<IDistributedCache>();
        var tenantCache = httpContext.RequestServices.GetRequiredService<ITenantCacheService>();
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<IdempotencyEndpointFilter>>();

        // ITenantCacheService prefixes keys with t:{tenantId}: automatically.
        // For the IDistributedCache probe-read we reconstruct the full key to match.
        var tenantId = httpContext.User.FindFirst("tenant")?.Value ?? "global";
        var logicalKey = CacheKeys.IdempotencyEntry(idempotencyKey);
        var fullKey = CacheKeys.IdempotencyEntryFull(tenantId, idempotencyKey);
        var tags = new[] { CacheKeys.Tags.Idempotency }; // tenant tag injected automatically by ITenantCacheService

        // Probe-only read: IDistributedCache has a real GetAsync that returns null on miss,
        // unlike HybridCache which requires a factory. We bypass L1 here because idempotency
        // replays are rare relative to first-calls and L1 warmth has little value.
        var cachedBytes = await distributedCache.GetAsync(fullKey, httpContext.RequestAborted).ConfigureAwait(false);
        if (cachedBytes is not null && cachedBytes.Length > 0)
        {
            var cached = JsonSerializer.Deserialize<CachedIdempotentResponse>(cachedBytes, JsonOpts);
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
        }

        // Execute the handler
        var result = await next(context).ConfigureAwait(false);

        // Cache the response through HybridCache so the tag invalidation path works for purges.
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
            // ITenantCacheService.SetAsync prefixes the logical key and adds the tenant tag automatically.
            await tenantCache.SetAsync(logicalKey, responseToCache, setOptions, tags, httpContext.RequestAborted).ConfigureAwait(false);
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
