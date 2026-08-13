using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Caching;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Identity.Claims;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using StackExchange.Redis;

namespace FSH.Framework.Web.Idempotency;

/// <summary>
/// Endpoint filter that provides idempotency for POST/PUT/PATCH requests.
/// When an Idempotency-Key header is present, the response is cached and replayed
/// for subsequent requests with the same key.
/// </summary>
/// <remarks>
/// Uses <see cref="IDistributedCache"/> for both the probe read and the write, on the same raw key
/// and serializer, so the two are symmetric (a HybridCache write keys its L2 entries under its own
/// scheme, which a raw-key probe never finds — replay then silently never engages).
/// The handler result is executed into a buffer so the cached payload is the real wire body and
/// status code (an <c>Ok&lt;T&gt;</c>/<c>Created&lt;T&gt;</c> wrapper would otherwise be serialized
/// verbatim, and <c>Response.StatusCode</c> is still the default at filter time — the IResult sets
/// it only when it executes). Concurrent duplicate keys are serialized by an atomic in-flight
/// reservation (Redis <c>SET NX</c> when a multiplexer is registered, an in-process set otherwise).
/// The stored response is written before the body reaches the client and with a token that cannot be
/// cancelled: it is the durable record that the side effect already happened, so it has to outlive the
/// request that produced it — a client that times out and retries is the commonest duplicate there is.
/// For the same reason the handler itself runs with the client's abort token detached, so a disconnect
/// mid-request cannot leave a committed side effect with no stored response behind it.
/// </remarks>
public sealed class IdempotencyEndpointFilter : IEndpointFilter
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // Response headers worth replaying. Allow-list rather than block-list: executing an IResult is
    // exactly when these get set (Created(uri, value) writes Location), and a replayed 201 without
    // Location breaks any client that follows it — under retry conditions nobody tests. Everything
    // else is either transport (Content-Length, Transfer-Encoding) or host-owned (Date, Server), and
    // replaying a stale value there corrupts the response.
    private static readonly string[] ReplayableHeaders = ["Location", "ETag"];

    // Compare-and-delete: a reservation is released only by the request that took it. An
    // unconditional delete lets a request that failed open, or one whose reservation already expired,
    // free a lock another request is still holding — and then a third request runs the handler too.
    private const string ReleaseIfOwnedScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    // In-process reservation used when no Redis multiplexer is registered. Single-instance only —
    // a multi-instance host in this stack already runs Redis (shared Data Protection key ring), so
    // the Redis branch below covers every deployment where cross-instance duplicates are possible.
    // ponytail: the multiplexer and the IDistributedCache are resolved independently, so a host that
    // configures Redis for one and not the other (quota Redis without caching Redis) gets a shared
    // lock over a per-process entry store. Cross-instance dedup needs the CACHE on Redis; the lock
    // alone cannot provide it.
    private static readonly ConcurrentDictionary<string, InFlightEntry> InFlight = new(StringComparer.Ordinal);

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
            // ProblemDetails, not a bare JSON string: every other error these endpoints can produce
            // goes out as RFC 9457 through the global handler, and a client parsing that shape chokes
            // on a naked string.
            return TypedResults.Problem(
                detail: $"Idempotency key exceeds maximum length of {options.MaxKeyLength}.",
                instance: httpContext.Request.Path,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Idempotency-Key");
        }

        var distributedCache = httpContext.RequestServices.GetRequiredService<IDistributedCache>();
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<IdempotencyEndpointFilter>>();

        var tenantId = ResolveTenant(httpContext);

        // Scope the entry to the caller and the operation, not the tenant alone. Keyed on the tenant
        // alone, one key reused across two idempotent endpoints replays the first endpoint's response
        // on the second — the request silently never runs — and two users of the same tenant who pick
        // the same low-entropy key ("1", "retry") on the same endpoint get each other's response
        // bodies while their own request is suppressed. That was harmless only while replay never
        // engaged; it does now. Anonymous endpoints (self-registration) resolve neither a tenant nor a
        // caller and share one bucket, so the operation is what keeps them apart from each other.
        var operation = $"{httpContext.Request.Method}:{RouteIdentity(httpContext)}";
        var cacheKey = CacheKeys.IdempotencyEntry(tenantId, $"{ResolveCaller(httpContext)}:{operation}:{idempotencyKey}");

        // Probe-only read via IDistributedCache (real GetAsync, null on miss — unlike HybridCache's
        // factory). Bypasses L1: replays are rare vs first-calls, so L1 warmth has little value.
        var cached = await ProbeAsync(distributedCache, cacheKey, logger, idempotencyKey, httpContext.RequestAborted).ConfigureAwait(false);
        if (cached is not null)
        {
            return await ReplayAsync(httpContext, cached, idempotencyKey, logger).ConfigureAwait(false);
        }

        // Atomically reserve the key so concurrent duplicates don't both execute the handler. The
        // lock lives under its own prefix rather than a suffix on the entry key: a caller-supplied
        // key ending in the suffix would otherwise land the lock exactly on another entry's key.
        // ponytail: the reservation is not renewed while the handler runs, so a handler slower than
        // ReservationTtl lets a duplicate through (the entry is not stored yet either, so the probe
        // can't catch it). Add lease renewal if an idempotent endpoint ever runs longer than that.
        var multiplexer = httpContext.RequestServices.GetService<IConnectionMultiplexer>();
        var reservationKey = "lock:" + cacheKey;
        var reservation = await TryReserveAsync(multiplexer, reservationKey, options.ReservationTtl, logger, idempotencyKey).ConfigureAwait(false);
        if (reservation.Denied)
        {
            // Another request with this key is in flight. It may have finished between the probe
            // and the reservation — re-probe once, otherwise report the in-progress conflict.
            var raced = await ProbeAsync(distributedCache, cacheKey, logger, idempotencyKey, httpContext.RequestAborted).ConfigureAwait(false);
            if (raced is not null)
            {
                return await ReplayAsync(httpContext, raced, idempotencyKey, logger).ConfigureAwait(false);
            }

            // "Retry shortly" is only actionable with a number on it. One second, not ReservationTtl:
            // the original is normally still running and about to store its response, and the TTL is
            // the worst case (the holder died) — telling every client to wait it out serializes them
            // behind a lock that has probably already been released.
            httpContext.Response.Headers.RetryAfter = "1";
            return TypedResults.Problem(
                detail: "A request with this Idempotency-Key is already being processed. Retry shortly.",
                instance: httpContext.Request.Path,
                statusCode: StatusCodes.Status409Conflict,
                title: "Idempotent request in progress");
        }

        try
        {
            // Probe again now that the key is held. The first probe and the reservation are two
            // steps, and the original request can store its response and release in between — the
            // duplicate would then take the freed lock and run the handler a second time.
            var settled = await ProbeAsync(distributedCache, cacheKey, logger, idempotencyKey, httpContext.RequestAborted).ConfigureAwait(false);
            if (settled is not null)
            {
                return await ReplayAsync(httpContext, settled, idempotencyKey, logger).ConfigureAwait(false);
            }

            // The handler runs with the client's abort token detached. It commits a side effect, and
            // the record of that side effect is the stored response — so the handler has to reach the
            // end even if the client hangs up mid-request. Left attached, a disconnect after the
            // commit cancels the next await inside the handler (an EF read, an outbox write, a
            // Mediator behaviour), the exception leaves the filter with nothing to store, and the
            // client's retry re-executes the side effect: the exact duplicate this filter is for.
            // The trade is that a client disconnect no longer aborts an idempotent handler.
            var originalAborted = httpContext.RequestAborted;
            object? result;
            try
            {
                httpContext.RequestAborted = CancellationToken.None;
                result = await next(context).ConfigureAwait(false);
            }
            finally
            {
                httpContext.RequestAborted = originalAborted;
            }

            // A handler that wrote the response itself (an HttpContext-taking handler returning null)
            // has already started it. Capturing is impossible at that point — the buffer swap comes
            // too late, so the entry would be an empty body replayed for the full TTL — and setting
            // the status below would throw. Hand the handler's own return back to the pipeline and
            // leave idempotency out of it.
            if (httpContext.Response.HasStarted)
            {
                // The route PATTERN, not the operation used for the cache key: the latter folds in the
                // request method, the resolved route values and the raw path, so logging it puts
                // caller-controlled text in a log line (CodeQL cs/log-forging). The pattern is a literal
                // from the route table and identifies the endpoint just as well for this warning.
                var routePattern = (httpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unknown";
                logger.LogWarning(
                    "Idempotent handler for {RoutePattern} started the response itself; nothing captured or stored for key {KeyHash}",
                    routePattern,
                    HashKey(idempotencyKey));

                // Empty rather than null when the handler returned nothing: a null return makes the
                // framework append a serialized "null" to what the handler already wrote.
                return result ?? Results.Empty;
            }

            // Execute the result into a buffer to capture the real wire body + status code, then
            // serve that buffer to the client. Returning the IResult unexecuted would leave
            // Response.StatusCode at its default and cache the wrapper object, not the wire body.
            var captured = await ExecuteAndCaptureAsync(result, httpContext).ConfigureAwait(false);

            httpContext.Response.StatusCode = captured.StatusCode;
            if (captured.ContentType is not null)
            {
                httpContext.Response.ContentType = captured.ContentType;
            }

            // Store BEFORE the body goes to the client, and only on success. The handler's side effect
            // has already committed at this point, so the record of it must not depend on the client
            // still being there; writing to a socket the client closed throws, and doing that first
            // would skip the store and let the retry re-execute the handler. Non-2xx is not a record
            // of a committed side effect — caching it would lock the key out for the full TTL after a
            // transient downstream failure, so a retry with the same key is allowed to run again.
            if (captured.StatusCode is >= 200 and < 300)
            {
                await CacheResponseAsync(distributedCache, cacheKey, captured, options.DefaultTtl, logger, idempotencyKey).ConfigureAwait(false);
            }

            if (captured.Body.Length > 0)
            {
                await httpContext.Response.Body.WriteAsync(captured.Body, httpContext.RequestAborted).ConfigureAwait(false);
            }

            // Response already written to the body directly; return an empty result so the framework
            // doesn't serialize a null return and append "null" after the captured payload.
            return Results.Empty;
        }
        finally
        {
            await ReleaseReservationAsync(multiplexer, reservationKey, reservation, logger, idempotencyKey).ConfigureAwait(false);
        }
    }

    // Write to the SAME store + key the probe reads. HybridCache.SetAsync keys its L2 entries under
    // its own scheme, so a raw-key IDistributedCache probe never found them and replay silently never
    // engaged. Idempotency entries are short-lived (TTL) and their tag-purge path was unused, so
    // IDistributedCache alone — symmetric with the probe — is correct.
    private static async ValueTask CacheResponseAsync(
        IDistributedCache distributedCache,
        string cacheKey,
        CachedIdempotentResponse response,
        TimeSpan ttl,
        ILogger logger,
        string idempotencyKey)
    {
        try
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(response, JsonOpts);

            // CancellationToken.None on purpose: RequestAborted is already signalled whenever this
            // matters (client hung up), and cancelling the store is what makes the retry re-execute.
            await distributedCache.SetAsync(
                cacheKey,
                payload,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                CancellationToken.None).ConfigureAwait(false);
        }
        // Best-effort caching: a store that is down degrades idempotency to a convenience rather
        // than 500ing a request whose side effect already committed. The token above is None, so
        // an OperationCanceledException here is not a client disconnect and is left to propagate.
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to cache idempotent response for key {KeyHash}", HashKey(idempotencyKey));
        }
    }

    private static async ValueTask<CachedIdempotentResponse?> ProbeAsync(
        IDistributedCache cache, string cacheKey, ILogger logger, string idempotencyKey, CancellationToken ct)
    {
        byte[]? bytes;
        try
        {
            bytes = await cache.GetAsync(cacheKey, ct).ConfigureAwait(false);
        }
        // Fail open here as well, or the probe is the one link that hard-fails the request: the
        // reservation and the store both degrade to a warning when the cache is down, while the probe
        // runs on EVERY keyed request — letting a connection error escape takes every idempotent
        // endpoint down for the clients that send a key, and leaves it up for the ones that don't.
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Idempotency probe failed for key {KeyHash}; treating it as a miss", HashKey(idempotencyKey));
            return null;
        }

        if (bytes is not { Length: > 0 })
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CachedIdempotentResponse>(bytes, JsonOpts);
        }
        // An entry that can't be read is a miss, not a 500. This path only became reachable once
        // replay started engaging at all, and a shared cache can hold an entry written by another
        // version or another writer at the same key — re-running the handler beats failing the request.
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Discarding unreadable idempotency entry for key {KeyHash}", HashKey(idempotencyKey));
            return null;
        }
    }

    private static async ValueTask<object?> ReplayAsync(
        HttpContext httpContext, CachedIdempotentResponse cached, string idempotencyKey, ILogger logger)
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

        foreach (var header in cached.Headers)
        {
            httpContext.Response.Headers[header.Key] = header.Value;
        }

        if (cached.Body.Length > 0)
        {
            await httpContext.Response.Body.WriteAsync(cached.Body, httpContext.RequestAborted).ConfigureAwait(false);
        }

        // Empty result (not null) so the framework doesn't append a serialized "null".
        return Results.Empty;
    }

    // ponytail: the whole response is buffered in memory with no size cap, and with the abort token
    // detached a result that never completes on its own never completes here either. Fine for the
    // small JSON payloads the idempotent endpoints return; do not put .WithIdempotency() on a
    // streaming, SSE or large-file endpoint. Add a size ceiling (skip the store, stream through)
    // before one exists.
    private static async Task<CachedIdempotentResponse> ExecuteAndCaptureAsync(
        object? result, HttpContext httpContext)
    {
        var originalBody = httpContext.Response.Body;
        var originalAborted = httpContext.RequestAborted;
        await using var buffer = new MemoryStream();
        httpContext.Response.Body = buffer;

        // Detach the client's abort token while capturing. The result is being written to an
        // in-memory buffer, never the socket, so a client that hung up must not truncate it — and
        // ASP.NET's WriteAsJsonAsync reads RequestAborted itself and swallows the cancellation, so
        // the capture would silently come back EMPTY and that empty body would be cached and
        // replayed for the full TTL.
        httpContext.RequestAborted = CancellationToken.None;
        try
        {
            switch (result)
            {
                case null:
                    break;
                case IResult endpointResult:
                    await endpointResult.ExecuteAsync(httpContext).ConfigureAwait(false);
                    break;
                default:
                    // A non-IResult return is serialized as JSON by the framework — mirror that.
                    await httpContext.Response.WriteAsJsonAsync(result, result.GetType(), options: null, contentType: null, CancellationToken.None).ConfigureAwait(false);
                    break;
            }

            var statusCode = httpContext.Response.StatusCode is > 0 and < 600
                ? httpContext.Response.StatusCode
                : StatusCodes.Status200OK;

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in ReplayableHeaders)
            {
                var value = httpContext.Response.Headers[name];
                if (!StringValues.IsNullOrEmpty(value))
                {
                    headers[name] = value.ToString();
                }
            }

            return new CachedIdempotentResponse
            {
                StatusCode = statusCode,
                // Left null when the result set none (204, an empty body): fabricating
                // "application/json" there would replay a content type for a response with no content.
                ContentType = httpContext.Response.ContentType,
                Body = buffer.ToArray(),
                Headers = headers,
            };
        }
        finally
        {
            httpContext.Response.Body = originalBody;
            httpContext.RequestAborted = originalAborted;
        }
    }

    private static async ValueTask<Reservation> TryReserveAsync(
        IConnectionMultiplexer? multiplexer, string reservationKey, TimeSpan ttl, ILogger logger, string idempotencyKey)
    {
        var token = Guid.NewGuid().ToString("N");

        if (multiplexer is not null)
        {
            try
            {
                var db = multiplexer.GetDatabase();
                return await db.StringSetAsync(reservationKey, token, ttl, When.NotExists).ConfigureAwait(false)
                    ? Reservation.Held(token)
                    : Reservation.Refused;
            }
            // Fail open on a Redis blip: the reservation is a concurrency convenience, not a correctness
            // requirement (the response cache still dedups later retries). Proceed rather than 500 the
            // request, matching the best-effort stance the response write already takes — but proceed
            // WITHOUT ownership, so the release can't delete a lock another request is holding.
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Idempotency reservation failed for key {KeyHash}; proceeding without it", HashKey(idempotencyKey));
                return Reservation.Unowned;
            }
        }

        return TryReserveInProcess(reservationKey, token, ttl);
    }

    // Mirrors the Redis branch: take the key if free, take it over if the holder's reservation has
    // outlived the TTL. Without the takeover a handler that never returns strands the key until the
    // process restarts, and every retry of it 409s forever — the Redis branch self-heals on expiry.
    private static Reservation TryReserveInProcess(string reservationKey, string token, TimeSpan ttl)
    {
        var ttlMs = (long)ttl.TotalMilliseconds;
        while (true)
        {
            var entry = new InFlightEntry(token, Environment.TickCount64);
            if (InFlight.TryAdd(reservationKey, entry))
            {
                return Reservation.Held(token);
            }

            if (!InFlight.TryGetValue(reservationKey, out var holder))
            {
                continue;
            }

            if (Environment.TickCount64 - holder.StartedAtMs < ttlMs)
            {
                return Reservation.Refused;
            }

            if (InFlight.TryUpdate(reservationKey, entry, holder))
            {
                return Reservation.Held(token);
            }
        }
    }

    private static async ValueTask ReleaseReservationAsync(
        IConnectionMultiplexer? multiplexer, string reservationKey, Reservation reservation, ILogger logger, string idempotencyKey)
    {
        if (reservation.Token is not { } token)
        {
            return;
        }

        if (multiplexer is not null)
        {
            try
            {
                await multiplexer.GetDatabase()
                    .ScriptEvaluateAsync(ReleaseIfOwnedScript, [reservationKey], [token])
                    .ConfigureAwait(false);
            }
            // Cancellation is swallowed too, not just faults: nothing here may throw out of the
            // finally, because by this point the response body has already gone to the client and an
            // exception can only reset the connection on a request that actually succeeded. The short
            // ReservationTtl expires a missed delete on its own.
            catch (OperationCanceledException)
            {
                // Shutdown or a cancelled Redis call — the reservation expires with its TTL.
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to release idempotency reservation for key {KeyHash}", HashKey(idempotencyKey));
            }

            return;
        }

        if (InFlight.TryGetValue(reservationKey, out var holder) && holder.Token == token)
        {
            InFlight.TryRemove(new KeyValuePair<string, InFlightEntry>(reservationKey, holder));
        }
    }

    // Tenant scope for the cache key: the resolved tenant context first, the claim only as a
    // fallback. The resolved context is the tenant the handler's side effect actually lands in
    // (BaseDbContext scopes its query filters off the same accessor), including the case where a root
    // operator scopes one request to another tenant — keyed on the claim alone, every tenant a root
    // operator touches would share one "root" bucket and a reused key would replay one tenant's
    // response body to another. The claim covers requests that carry a JWT but no tenant header:
    // Finbuckle's claim strategy runs before authentication, so it resolves nothing for them.
    // The raw header is deliberately NOT a fallback — an unresolved header is one Finbuckle refused
    // (no such tenant), and an unvalidated caller-supplied value has no business in a shared key.
    private static string ResolveTenant(HttpContext httpContext)
    {
        var resolved = httpContext.RequestServices
            .GetService<IMultiTenantContextAccessor<AppTenantInfo>>()?.MultiTenantContext?.TenantInfo?.Id;
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            return resolved;
        }

        var fromClaim = httpContext.User.FindFirst(ClaimConstants.Tenant)?.Value;
        return string.IsNullOrWhiteSpace(fromClaim) ? "global" : fromClaim;
    }

    // The caller, so one tenant's users don't share an entry. Falls back to the tenant-wide bucket
    // for anonymous endpoints, which have no caller to scope by.
    private static string ResolveCaller(HttpContext httpContext)
    {
        var userId = httpContext.User.GetUserId();
        return string.IsNullOrWhiteSpace(userId) ? "anon" : userId;
    }

    // The route pattern PLUS its resolved values — the pattern alone makes PUT /tickets/1 and
    // PUT /tickets/2 the same operation, so one key reused across two resources replays the first
    // one's response and the second update silently never runs. The values are the parsed ones, not
    // the raw path, so a retry of the same request matches while a different resource does not.
    // The request body is deliberately not part of it: reading it here would buffer every payload,
    // so the same key against the same resource with a changed body still replays (documented).
    private static string RouteIdentity(HttpContext httpContext)
    {
        var pattern = (httpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? httpContext.Request.Path.ToString();
        var values = httpContext.Request.RouteValues;
        if (values.Count == 0)
        {
            return pattern;
        }

        var resolved = values
            .Where(value => value.Value is not null)
            .OrderBy(value => value.Key, StringComparer.Ordinal)
            .Select(value => $"{value.Key}={value.Value}");

        return $"{pattern}[{string.Join('&', resolved)}]";
    }

    private static string HashKey(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }

    /// <summary>
    /// Outcome of an in-flight reservation attempt. <c>Token</c> is the proof of ownership: it is
    /// null when the reservation was refused (a duplicate is running) and also when the store failed
    /// and we proceeded without one, so neither case releases a lock it does not hold.
    /// </summary>
    private readonly record struct Reservation(bool Denied, string? Token)
    {
        public static Reservation Refused => new(Denied: true, Token: null);

        public static Reservation Unowned => new(Denied: false, Token: null);

        public static Reservation Held(string token) => new(Denied: false, token);
    }

    private readonly record struct InFlightEntry(string Token, long StartedAtMs);
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

        // The marker is what makes the wiring inspectable: an endpoint filter leaves no metadata, so
        // without it nothing can assert which endpoints are idempotent — including the rule that an
        // anonymous endpoint must not be, since every unauthenticated caller shares one cache bucket.
        return builder
            .WithMetadata(IdempotentEndpointMetadata.Instance)
            .AddEndpointFilter<IdempotencyEndpointFilter>();
    }
}
