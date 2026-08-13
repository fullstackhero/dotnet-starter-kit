using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Caching;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web.Idempotency;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;

namespace Framework.Tests.Web;

/// <summary>
/// Regression for audit findings API-01 (idempotency replay stored the wrong wire shape and status)
/// and CONC-01 (no in-flight reservation, so concurrent duplicate keys executed twice). These
/// exercise the REAL <see cref="IdempotencyEndpointFilter"/> against a real in-memory
/// <see cref="IDistributedCache"/> — the same store the filter now uses for both probe and write.
/// </summary>
public sealed class IdempotencyEndpointFilterReplayTests
{
    // Per-instance, not a shared const: the filter's in-process reservation set is static and
    // process-wide, so a key shared across tests would surface a leak in one as a phantom 409 in
    // another — and xUnit runs test classes in parallel.
    private readonly string Key = Guid.NewGuid().ToString("N");

    // Mirrors the serializer the filter stores entries with.
    private static readonly JsonSerializerOptions CacheJsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // ─── API-01: replayed status ─────────────────────────────────────

    [Fact]
    public async Task Replay_Should_PreserveCreatedStatus_When_FirstResponseWas201()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();
        var id = Guid.NewGuid();

        // First call: handler returns a 201 Created (the framework would execute it AFTER the filter
        // returns — so at cache time Response.StatusCode is still the default 200).
        var first = NewContext(provider);
        await filter.InvokeAsync(
            new TestFilterContext(first),
            _ => ValueTask.FromResult<object?>(TypedResults.Created($"/samples/{id}", new SampleDto(id, "widget"))));

        // Second call, same key: must replay.
        var replayBody = new MemoryStream();
        var second = NewContext(provider, replayBody);
        await filter.InvokeAsync(
            new TestFilterContext(second),
            _ => throw new InvalidOperationException("handler must NOT run on an idempotent replay"));

        second.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeTrue(
            "sanity: the replay path must actually engage, otherwise this test would be vacuous");
        second.Response.StatusCode.ShouldBe(
            StatusCodes.Status201Created,
            "a correct replay must reproduce the original 201 Created — the filter captures Response.StatusCode " +
            "BEFORE the IResult executes, so it caches (and replays) 200 instead.");
    }

    // ─── API-01: replayed body wire shape ────────────────────────────

    [Fact]
    public async Task Replay_Should_ReturnPlainDtoBody_Not_WrappedIResult()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();
        var id = Guid.NewGuid();

        var first = NewContext(provider);
        await filter.InvokeAsync(
            new TestFilterContext(first),
            _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(id, "widget"))));

        var replayBody = new MemoryStream();
        var second = NewContext(provider, replayBody);
        await filter.InvokeAsync(
            new TestFilterContext(second),
            _ => throw new InvalidOperationException("handler must NOT run on an idempotent replay"));

        replayBody.Position = 0;
        using var doc = JsonDocument.Parse(replayBody.ToArray());

        doc.RootElement.TryGetProperty("value", out _).ShouldBeFalse(
            "a correct replay body is the wire DTO; the filter caches SerializeToUtf8Bytes(result) where " +
            "result is the wrapped Ok<T>/Created<T>, leaking the {\"value\":...} envelope onto the wire.");
        doc.RootElement.TryGetProperty("id", out _).ShouldBeTrue(
            "the plain DTO's own properties should be at the JSON root");
    }

    // ─── CONC-01: no in-flight reservation ───────────────────────────

    [Fact]
    public async Task Filter_Should_ExecuteHandlerOnce_When_TwoConcurrentRequestsShareKey()
    {
        // Explicit, generous ReservationTtl rather than the 1-minute default: the assertion is about
        // the lock holding, and on the default a CI freeze longer than the TTL would hand the key over
        // and turn a real pass into a flake.
        var provider = BuildProvider(new IdempotencyOptions { ReservationTtl = TimeSpan.FromMinutes(30) }, multiplexer: null);
        var filter = new IdempotencyEndpointFilter();

        int executions = 0;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // First request enters the handler and holds the in-flight reservation until released.
        EndpointFilterDelegate first = async _ =>
        {
            Interlocked.Increment(ref executions);
            started.SetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            return TypedResults.Ok(new SampleDto(Guid.NewGuid(), "first"));
        };

        // Second request shares the key; its handler must never run while the first is in flight.
        EndpointFilterDelegate second = _ =>
        {
            Interlocked.Increment(ref executions);
            return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "second")));
        };

        var firstCall = filter.InvokeAsync(new TestFilterContext(NewContext(provider)), first).AsTask();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5)); // first now holds the reservation

        var secondResult = await filter.InvokeAsync(new TestFilterContext(NewContext(provider)), second);

        release.SetResult();
        await firstCall.WaitAsync(TimeSpan.FromSeconds(10));

        executions.ShouldBe(
            1,
            "an idempotent endpoint must execute the handler exactly once for concurrent duplicate keys; " +
            "the second request should be rejected while the first is in flight.");
        // Cast, don't null-conditional: `as ... ?.ShouldBe(...)` skips the assertion entirely for a
        // result that isn't an IStatusCodeHttpResult — the check evaporates exactly when it's broken.
        secondResult.ShouldBeAssignableTo<IStatusCodeHttpResult>()!
            .StatusCode.ShouldBe(
                StatusCodes.Status409Conflict,
                "a concurrent duplicate that arrives while the original is still running gets 409 Conflict.");
    }

    // ─── HIGH: reservation TTL is the short ReservationTtl, not the 24h response TTL ─────

    [Fact]
    public async Task Reservation_Should_UseReservationTtl_Not_ResponseTtl()
    {
        var options = new IdempotencyOptions
        {
            ReservationTtl = TimeSpan.FromSeconds(37), // distinct from DefaultTtl to prove which one is used
        };
        var db = Substitute.For<IDatabase>();
        TimeSpan? capturedTtl = null;
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(ci => { capturedTtl = ci.ArgAt<TimeSpan?>(2); return Task.FromResult(true); });
        var provider = BuildProvider(options, RedisMultiplexer(db));
        var filter = new IdempotencyEndpointFilter();

        await filter.InvokeAsync(
            new TestFilterContext(NewContext(provider)),
            _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget"))));

        capturedTtl.ShouldBe(
            options.ReservationTtl,
            "the in-flight reservation must use the short ReservationTtl; keying it to the 24h response TTL " +
            "would strand the lock for a day if the process is killed before the finally-release runs.");
        capturedTtl.ShouldNotBe(options.DefaultTtl);
    }

    // ─── MEDIUM + nit: a Redis fault on reserve/release fails open, never 500s ───────────

    [Fact]
    public async Task Filter_Should_ProceedWithoutThrowing_When_RedisReservationFaults()
    {
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(Task.FromException<bool>(new RedisException("reserve blip")));
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromException<bool>(new RedisException("release blip")));
        var provider = BuildProvider(new IdempotencyOptions(), RedisMultiplexer(db));
        var filter = new IdempotencyEndpointFilter();

        int executions = 0;
        var result = await filter.InvokeAsync(
            new TestFilterContext(NewContext(provider)),
            _ => { executions++; return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget"))); });

        executions.ShouldBe(
            1,
            "a transient Redis error on the reservation must fail open — the handler still runs. On main " +
            "idempotency degraded gracefully; treating the reservation as authoritative would 500 the request.");
        result.ShouldNotBeNull("the request must complete normally, not throw out of the filter");
    }

    // ─── MEDIUM: replay must carry the headers the IResult set (Location on 201) ─────────

    [Fact]
    public async Task Replay_Should_PreserveLocationHeader_When_FirstResponseWasCreated()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();
        var id = Guid.NewGuid();
        var location = $"/samples/{id}";

        var first = NewContext(provider, new MemoryStream());
        await filter.InvokeAsync(
            new TestFilterContext(first),
            _ => ValueTask.FromResult<object?>(TypedResults.Created(location, new SampleDto(id, "widget"))));

        first.Response.Headers.Location.ToString().ShouldBe(
            location,
            "sanity: executing Created(uri, value) is what sets Location, so the first call must have it");

        var second = NewContext(provider, new MemoryStream());
        await filter.InvokeAsync(
            new TestFilterContext(second),
            _ => throw new InvalidOperationException("handler must NOT run on an idempotent replay"));

        second.Response.Headers.Location.ToString().ShouldBe(
            location,
            "a replayed 201 without Location breaks any client that follows the header — and only under " +
            "the retry conditions nobody tests. The captured response must carry the meaningful headers.");
    }

    // ─── HIGH: the stored response must outlive the request that produced it ─────────────

    [Fact]
    public async Task FirstCall_Should_StillCacheResponse_When_ClientDisconnectsAfterHandlerRan()
    {
        // TokenSensitiveCache, not the plain in-memory one: MemoryDistributedCache ignores the token,
        // so against it this test passes whether the store uses CancellationToken.None or the
        // cancelled RequestAborted — it would assert nothing about the fix it exists to pin.
        var provider = BuildProviderWith(new TokenSensitiveCache(NewMemoryCache()));
        var filter = new IdempotencyEndpointFilter();
        var id = Guid.NewGuid();

        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        var first = NewContext(provider, new MemoryStream());
        first.RequestAborted = aborted.Token;

        try
        {
            await filter.InvokeAsync(
                new TestFilterContext(first),
                _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(id, "widget"))));
        }
        catch (OperationCanceledException)
        {
            // Writing to a socket the client already closed is allowed to fail — the handler's side
            // effect has committed by then, so the stored response must survive it regardless.
        }

        var replayBody = new MemoryStream();
        var second = NewContext(provider, replayBody);
        await filter.InvokeAsync(
            new TestFilterContext(second),
            _ => throw new InvalidOperationException(
                "handler must NOT re-run: client-timeout-then-retry is the exact duplicate this feature defends against"));

        second.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeTrue(
            "the store must not be tied to the client's connection: if it is, a client that times out and " +
            "retries re-executes the handler — the single most common way a duplicate is generated.");

        using var doc = JsonDocument.Parse(replayBody.ToArray());
        doc.RootElement.GetProperty("id").GetGuid().ShouldBe(
            id,
            "capturing under RequestAborted is worse than not caching: WriteAsJsonAsync swallows the " +
            "cancellation, so an EMPTY body gets stored and replayed as a 200 for the full TTL.");
    }

    // ─── a bodiless success must not gain a content type it never had ───────────────────

    [Fact]
    public async Task Replay_Should_NotInventContentType_When_FirstResponseWasNoContent()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();

        var first = NewContext(provider, new MemoryStream());
        await filter.InvokeAsync(
            new TestFilterContext(first),
            _ => ValueTask.FromResult<object?>(TypedResults.NoContent()));

        first.Response.ContentType.ShouldBeNull("sanity: a 204 carries no content type");

        var second = NewContext(provider, new MemoryStream());
        await filter.InvokeAsync(
            new TestFilterContext(second),
            _ => throw new InvalidOperationException("handler must NOT run on an idempotent replay"));

        second.Response.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
        second.Response.ContentType.ShouldBeNull(
            "defaulting the captured content type to application/json replays a 204 that advertises a JSON " +
            "body it does not have.");
    }

    // ─── note: a failure response must not lock the key out for the full 24h TTL ─────────

    [Fact]
    public async Task Filter_Should_NotCacheResponse_When_FirstResponseIsNotSuccessful()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();

        var first = NewContext(provider, new MemoryStream());
        await filter.InvokeAsync(
            new TestFilterContext(first),
            _ => ValueTask.FromResult<object?>(TypedResults.Conflict("downstream busy")));

        int executions = 0;
        var second = NewContext(provider, new MemoryStream());
        await filter.InvokeAsync(
            new TestFilterContext(second),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget")));
            });

        executions.ShouldBe(
            1,
            "caching a non-2xx locks the caller out of retrying that key for the full response TTL (24h) after " +
            "a transient downstream failure. Only a successful response is a record of a committed side effect.");
    }

    // ─── an unreadable entry is a miss, not a 500 ───────────────────────────────────────

    [Fact]
    public async Task Filter_Should_RunHandler_When_CachedEntryIsUnreadable()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();
        var cache = provider.GetRequiredService<IDistributedCache>();

        // The key the filter reads: tenant + operation + caller key. Proven below by seeding a VALID
        // entry at it first — otherwise a wrong key here would make the real assertion pass as a
        // plain cache miss and the test would assert nothing.
        var storedKey = CacheKeys.IdempotencyEntry("global", $"anon:POST::{Key}");
        await cache.SetAsync(
            storedKey,
            JsonSerializer.SerializeToUtf8Bytes(
                new CachedIdempotentResponse { StatusCode = StatusCodes.Status200OK, Body = "{}"u8.ToArray() },
                CacheJsonOpts),
            new DistributedCacheEntryOptions());

        var seeded = NewContext(provider, new MemoryStream());
        await filter.InvokeAsync(
            new TestFilterContext(seeded),
            _ => throw new InvalidOperationException("sanity: a valid entry at this key must replay"));
        seeded.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeTrue(
            "sanity: this is the key the filter probes");

        await cache.SetAsync(storedKey, "{ this is not the cached shape"u8.ToArray(), new DistributedCacheEntryOptions());

        int executions = 0;
        var context = NewContext(provider, new MemoryStream());
        await filter.InvokeAsync(
            new TestFilterContext(context),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget")));
            });

        executions.ShouldBe(
            1,
            "an entry written by another version or another writer at the same key must degrade to a cache " +
            "miss; letting JsonException escape turns a shared-cache accident into a 500 on every retry.");
    }

    // ─── one key reused across two endpoints must not replay the other's response ────────

    [Fact]
    public async Task Filter_Should_NotReplayAcrossEndpoints_When_SameKeyIsReused()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();
        var ticketId = Guid.NewGuid();

        var onTickets = NewContext(provider, new MemoryStream());
        onTickets.SetEndpoint(RouteEndpointFor("api/v1/tickets"));
        await filter.InvokeAsync(
            new TestFilterContext(onTickets),
            _ => ValueTask.FromResult<object?>(TypedResults.Created($"/tickets/{ticketId}", new SampleDto(ticketId, "ticket"))));

        int executions = 0;
        var onBrands = NewContext(provider, new MemoryStream());
        onBrands.SetEndpoint(RouteEndpointFor("api/v1/catalog/brands"));
        await filter.InvokeAsync(
            new TestFilterContext(onBrands),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Created("/brands/1", new SampleDto(Guid.NewGuid(), "brand")));
            });

        executions.ShouldBe(
            1,
            "keyed on tenant + key alone, a key reused against a second idempotent endpoint replays the " +
            "first endpoint's response and the second request silently never runs. 31 endpoints in this " +
            "repo share that namespace, and one of them is anonymous (self-registration, no tenant claim).");
        onBrands.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeFalse();
    }

    // ─── the entry is scoped to the tenant the side effect lands in, not the caller's claim ──

    [Fact]
    public async Task Filter_Should_ScopeEntryToResolvedTenant_Not_CallerClaim()
    {
        var filter = new IdempotencyEndpointFilter();
        var cache = NewMemoryCache();

        // A root operator scoping one request to tenant "acme" via header: the claim stays "root",
        // Finbuckle resolves the target, and the handler writes into acme's data.
        var onAcme = NewContext(BuildProviderWith(cache, TenantContext("acme")), new MemoryStream());
        onAcme.User = TenantPrincipal("root");
        await filter.InvokeAsync(
            new TestFilterContext(onAcme),
            _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "acme-order"))));

        // Same operator, same key, now scoped to a different tenant.
        int executions = 0;
        var onGlobex = NewContext(BuildProviderWith(cache, TenantContext("globex")), new MemoryStream());
        onGlobex.User = TenantPrincipal("root");
        await filter.InvokeAsync(
            new TestFilterContext(onGlobex),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "globex-order")));
            });

        executions.ShouldBe(
            1,
            "keyed on the claim, every tenant a root operator touches shares one \"root\" bucket: the second " +
            "request replays acme's response body to globex and never runs. The entry must follow the resolved " +
            "tenant, which is the one BaseDbContext scopes the side effect to.");
        onGlobex.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeFalse();
    }

    [Fact]
    public async Task Filter_Should_IgnoreUnresolvedTenantHeader_When_BuildingTheKey()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();

        // No claim and no resolved tenant context: the header names a tenant Finbuckle refused (no
        // such tenant), so it is caller-supplied and unvalidated.
        var context = NewContext(provider, new MemoryStream());
        context.Request.Headers[MultitenancyConstants.Identifier] = "acme";
        await filter.InvokeAsync(
            new TestFilterContext(context),
            _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget"))));

        var cache = provider.GetRequiredService<IDistributedCache>();
        (await cache.GetAsync(CacheKeys.IdempotencyEntry("global", $"anon:POST::{Key}"))).ShouldNotBeNull(
            "an unvalidated header must not choose the bucket: taking it would let any caller write into — " +
            "and replay out of — a real tenant's idempotency namespace.");
        (await cache.GetAsync(CacheKeys.IdempotencyEntry("acme", $"anon:POST::{Key}"))).ShouldBeNull();
    }

    // ─── the reservation must be re-probed: the original can settle between probe and reserve ───

    [Fact]
    public async Task Filter_Should_Replay_When_OriginalSettledBetweenProbeAndReservation()
    {
        var inner = NewMemoryCache();
        var id = Guid.NewGuid();

        // Simulates the original request finishing in the window between the first probe (a miss) and
        // the reservation: the entry appears, and the lock it held is already released.
        var racing = new ProbeHookCache(inner);
        racing.SeedOnNextProbe(key => SeedEntryAsync(inner, key, id));

        var filter = new IdempotencyEndpointFilter();
        int executions = 0;
        var context = NewContext(BuildProviderWith(racing), new MemoryStream());
        await filter.InvokeAsync(
            new TestFilterContext(context),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "duplicate")));
            });

        executions.ShouldBe(
            0,
            "probing once before the reservation leaves a window: the original stores its response and " +
            "releases the lock in it, the duplicate then takes the free lock and executes the handler a " +
            "second time. The probe has to be repeated once the key is held.");
        context.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeTrue();
    }

    // ─── one caller's stored entry must not land on another key's lock ──────────────────

    [Fact]
    public async Task Filter_Should_NotBlockKey_When_AnotherKeysEntryLandsWhereItsLockWouldGo()
    {
        // Entries and locks share one Redis keyspace in production (IDistributedCache writes the entry
        // under its raw key — RedisCacheOptions.InstanceName is empty by default — and the reservation
        // is a StringSet on the same connection), so the fake shares one dictionary between the two.
        var keyspace = new SharedKeyspace();
        var provider = BuildProviderWith(new KeyspaceCache(keyspace), KeyspaceMultiplexer(keyspace));
        var filter = new IdempotencyEndpointFilter();

        // A completed request under the caller-supplied key "<key>:inflight" leaves a stored entry
        // behind. Under the suffix scheme that entry sits exactly where the lock for "<key>" goes.
        await filter.InvokeAsync(
            new TestFilterContext(NewContextForKey(provider, $"{Key}:inflight")),
            _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "poisoner"))));

        int executions = 0;
        var victim = NewContextForKey(provider, Key);
        var result = await filter.InvokeAsync(
            new TestFilterContext(victim),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "victim")));
            });

        executions.ShouldBe(
            1,
            "with the lock stored as \"<entry key>:inflight\", one request with a key ending in \":inflight\" " +
            "parks a 24h entry on another key's lock: every later request with that key sees the reservation " +
            "taken and 409s for the whole response TTL. The lock needs its own prefix.");
        (result as IStatusCodeHttpResult)?.StatusCode.ShouldNotBe(StatusCodes.Status409Conflict);
    }

    // ─── a reservation is released only by the request that owns it ──────────────────────

    [Fact]
    public async Task Release_Should_UseTheOwnershipToken_When_ReservationWasHeld()
    {
        var db = Substitute.For<IDatabase>();
        RedisValue storedToken = RedisValue.Null;
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(ci => { storedToken = ci.ArgAt<RedisValue>(1); return Task.FromResult(true); });
        db.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(RedisResult.Create(1)));
        var provider = BuildProvider(new IdempotencyOptions(), RedisMultiplexer(db));
        var filter = new IdempotencyEndpointFilter();

        await filter.InvokeAsync(
            new TestFilterContext(NewContext(provider, new MemoryStream())),
            _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget"))));

        // The script text is asserted, not just the fact that a script ran: an unconditional
        // `del` passed through the same call would satisfy a script-agnostic assertion while
        // deleting a lock a second request now owns.
        await db.Received(1).ScriptEvaluateAsync(
            Arg.Is<string>(script =>
                script.Contains("get", StringComparison.Ordinal) &&
                script.Contains("ARGV[1]", StringComparison.Ordinal) &&
                script.Contains("del", StringComparison.Ordinal)),
            Arg.Any<RedisKey[]>(),
            Arg.Is<RedisValue[]>(values => values.Length == 1 && values[0] == storedToken),
            Arg.Any<CommandFlags>());
        await db.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task Release_Should_DeleteNothing_When_ReservationFailedOpen()
    {
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(Task.FromException<bool>(new RedisException("reserve blip")));
        var provider = BuildProvider(new IdempotencyOptions(), RedisMultiplexer(db));
        var filter = new IdempotencyEndpointFilter();

        await filter.InvokeAsync(
            new TestFilterContext(NewContext(provider, new MemoryStream())),
            _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget"))));

        await db.DidNotReceive().ScriptEvaluateAsync(
            Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>());
        await db.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    // ─── the in-process reservation self-heals on TTL, like the Redis one ────────────────

    [Fact]
    public async Task Reservation_Should_BeRetaken_When_TheHolderOutlivesTheReservationTtl()
    {
        var provider = BuildProvider(new IdempotencyOptions { ReservationTtl = TimeSpan.Zero }, multiplexer: null);
        var filter = new IdempotencyEndpointFilter();

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var holderCall = filter.InvokeAsync(
            new TestFilterContext(NewContext(provider, new MemoryStream())),
            async _ =>
            {
                started.SetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                return TypedResults.Ok(new SampleDto(Guid.NewGuid(), "holder"));
            }).AsTask();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        int executions = 0;
        var result = await filter.InvokeAsync(
            new TestFilterContext(NewContext(provider, new MemoryStream())),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "second")));
            });

        release.SetResult();
        await holderCall.WaitAsync(TimeSpan.FromSeconds(10));

        executions.ShouldBe(
            1,
            "the in-process set has no expiry of its own: without the TTL takeover a handler that never " +
            "returns strands the key until the process restarts and every retry 409s forever, while the " +
            "Redis branch self-heals when the reservation expires.");
        (result as IStatusCodeHttpResult)?.StatusCode.ShouldNotBe(StatusCodes.Status409Conflict);
    }

    // ─── the handler must reach the end even if the client hangs up ─────────────────────

    [Fact]
    public async Task Handler_Should_RunToCompletion_When_ClientDisconnectsMidRequest()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();
        var id = Guid.NewGuid();

        using var aborted = new CancellationTokenSource();
        var first = NewContext(provider, new MemoryStream());
        first.RequestAborted = aborted.Token;

        var committed = false;
        try
        {
            await filter.InvokeAsync(
                new TestFilterContext(first),
                async invocation =>
                {
                    // The side effect commits, then the client gives up — and the handler still has
                    // work to do (an EF read, an outbox write, a Mediator behaviour), all of which
                    // observe HttpContext.RequestAborted.
                    await aborted.CancelAsync().ConfigureAwait(false);
                    invocation.HttpContext.RequestAborted.ThrowIfCancellationRequested();
                    committed = true;
                    return TypedResults.Ok(new SampleDto(id, "widget"));
                });
        }
        catch (OperationCanceledException)
        {
            // Writing the body to a socket the client closed is allowed to fail.
        }

        committed.ShouldBeTrue(
            "the handler must not be cancelled by the client disconnect: it has already committed, and " +
            "the stored response is the only record of that. Cancelled mid-handler, nothing is stored " +
            "and the client's retry re-executes the side effect.");

        var replayBody = new MemoryStream();
        var second = NewContext(provider, replayBody);
        await filter.InvokeAsync(
            new TestFilterContext(second),
            _ => throw new InvalidOperationException("handler must NOT re-run after a client disconnect"));

        second.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeTrue();
    }

    // ─── a handler that writes the response itself is left alone ────────────────────────

    [Fact]
    public async Task Filter_Should_PassThrough_When_HandlerStartedTheResponseItself()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();

        // DefaultHttpContext's own response feature reports HasStarted = false forever, so the
        // scenario needs a feature that says what a real server says once bytes are on the wire.
        var body = new MemoryStream();
        var context = NewStartedResponseContext(provider, body);
        var result = await filter.InvokeAsync(
            new TestFilterContext(context),
            async invocation =>
            {
                await invocation.HttpContext.Response.Body.WriteAsync("written-by-the-handler"u8.ToArray()).ConfigureAwait(false);
                return null;
            });

        result.ShouldNotBeNull("a null return would make the framework append a serialized \"null\" to the handler's own output");

        var cache = provider.GetRequiredService<IDistributedCache>();
        (await cache.GetAsync(CacheKeys.IdempotencyEntry("global", $"anon:POST::{Key}"))).ShouldBeNull(
            "the buffer swap comes after the handler ran, so a handler that started the response leaves " +
            "an EMPTY capture — storing it would replay a blank 200 for the full TTL, and setting the " +
            "captured status on an already-started response throws.");
    }

    // ─── a cache that is down degrades idempotency, it does not 500 the request ─────────

    [Fact]
    public async Task Filter_Should_RunHandler_When_TheProbeItselfFails()
    {
        var provider = BuildProviderWith(new FaultyCache());
        var filter = new IdempotencyEndpointFilter();

        int executions = 0;
        var result = await filter.InvokeAsync(
            new TestFilterContext(NewContext(provider, new MemoryStream())),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget")));
            });

        executions.ShouldBe(
            1,
            "the reservation and the store both fail open, so the probe must too: it runs on every keyed " +
            "request, and letting a connection error escape takes every idempotent endpoint down for the " +
            "clients that send a key while the ones that don't keep working.");
        result.ShouldNotBeNull();
    }

    // ─── same key, different resource: the second request must still run ────────────────

    [Fact]
    public async Task Filter_Should_NotReplayAcrossRouteValues_When_SameKeyTargetsAnotherResource()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();

        var onFirstTicket = NewContext(provider, new MemoryStream());
        onFirstTicket.SetEndpoint(RouteEndpointFor("api/v1/tickets/{id}"));
        onFirstTicket.Request.RouteValues["id"] = "1";
        await filter.InvokeAsync(
            new TestFilterContext(onFirstTicket),
            _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "ticket-1"))));

        int executions = 0;
        var onSecondTicket = NewContext(provider, new MemoryStream());
        onSecondTicket.SetEndpoint(RouteEndpointFor("api/v1/tickets/{id}"));
        onSecondTicket.Request.RouteValues["id"] = "2";
        await filter.InvokeAsync(
            new TestFilterContext(onSecondTicket),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "ticket-2")));
            });

        executions.ShouldBe(
            1,
            "keyed on the route pattern alone, PUT /tickets/1 and PUT /tickets/2 are the same operation: " +
            "the second request replays the first ticket's response and its own update never runs.");
        onSecondTicket.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeFalse();
    }

    // ─── two users of one tenant must not share an entry ───────────────────────────────

    [Fact]
    public async Task Filter_Should_ScopeEntryToTheCaller_When_TwoUsersShareATenantAndAKey()
    {
        var cache = NewMemoryCache();
        var filter = new IdempotencyEndpointFilter();

        var byAlice = NewContext(BuildProviderWith(cache, TenantContext("acme")), new MemoryStream());
        byAlice.User = CallerPrincipal("acme", userId: "alice");
        await filter.InvokeAsync(
            new TestFilterContext(byAlice),
            _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "alice-order"))));

        int executions = 0;
        var byBob = NewContext(BuildProviderWith(cache, TenantContext("acme")), new MemoryStream());
        byBob.User = CallerPrincipal("acme", userId: "bob");
        await filter.InvokeAsync(
            new TestFilterContext(byBob),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "bob-order")));
            });

        executions.ShouldBe(
            1,
            "two users of the same tenant who pick the same low-entropy key on the same endpoint would " +
            "otherwise get each other's response body while their own request is silently suppressed.");
        byBob.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeFalse();
    }

    // ─── the 409 has to say how long to wait ───────────────────────────────────────────

    [Fact]
    public async Task Conflict_Should_CarryRetryAfter_When_ADuplicateIsStillInFlight()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var holderCall = filter.InvokeAsync(
            new TestFilterContext(NewContext(provider, new MemoryStream())),
            async _ =>
            {
                started.SetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                return TypedResults.Ok(new SampleDto(Guid.NewGuid(), "holder"));
            }).AsTask();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var duplicate = NewContext(provider, new MemoryStream());
        var result = await filter.InvokeAsync(
            new TestFilterContext(duplicate),
            _ => throw new InvalidOperationException("the duplicate's handler must not run"));

        release.SetResult();
        await holderCall.WaitAsync(TimeSpan.FromSeconds(10));

        result.ShouldBeAssignableTo<IStatusCodeHttpResult>()!.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        duplicate.Response.Headers.RetryAfter.ToString().ShouldBe(
            "1",
            "\"retry shortly\" is only actionable with a number on it, and the original is normally about " +
            "to store its response — the reservation TTL is the worst case, not the hint.");
    }

    // ─── a handler that throws must not strand the key ─────────────────────────────────

    [Fact]
    public async Task Filter_Should_ReleaseTheReservation_When_TheHandlerThrows()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();

        var thrown = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await filter.InvokeAsync(
                new TestFilterContext(NewContext(provider, new MemoryStream())),
                _ => throw new InvalidOperationException("handler blew up")));

        thrown.Message.ShouldBe(
            "handler blew up",
            "the filter must not swallow a handler exception — the global handler turns it into ProblemDetails");

        int executions = 0;
        var retry = NewContext(provider, new MemoryStream());
        var result = await filter.InvokeAsync(
            new TestFilterContext(retry),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget")));
            });

        executions.ShouldBe(
            1,
            "released only on the success path, a handler that throws (validation, DB down) strands the " +
            "reservation and every retry of that key 409s until the TTL expires. The release belongs in " +
            "the finally.");
        result.ShouldBeOfType<EmptyHttpResult>();
    }

    // ─── the claim fallback still partitions tenants ───────────────────────────────────

    [Fact]
    public async Task Filter_Should_ScopeEntryToTheTenantClaim_When_NoTenantContextIsResolved()
    {
        var cache = NewMemoryCache();
        var filter = new IdempotencyEndpointFilter();

        // A JWT-only request: no tenant header, so Finbuckle's claim strategy (which runs before
        // authentication) resolved nothing and the claim is all there is.
        var fromAcme = NewContext(BuildProviderWith(cache), new MemoryStream());
        fromAcme.User = CallerPrincipal("acme", userId: "shared-integration");
        await filter.InvokeAsync(
            new TestFilterContext(fromAcme),
            _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "acme-order"))));

        int executions = 0;
        var fromGlobex = NewContext(BuildProviderWith(cache), new MemoryStream());
        fromGlobex.User = CallerPrincipal("globex", userId: "shared-integration");
        await filter.InvokeAsync(
            new TestFilterContext(fromGlobex),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "globex-order")));
            });

        executions.ShouldBe(
            1,
            "collapsing the claim fallback to \"global\" puts every JWT-only caller in one bucket: same " +
            "key, same route, and tenant A's response body replays to tenant B.");
        fromGlobex.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeFalse();
    }

    // ─── the denied duplicate re-probes before it 409s ─────────────────────────────────

    [Fact]
    public async Task DeniedDuplicate_Should_Replay_When_TheOriginalSettledWhileItWasBeingRefused()
    {
        var inner = NewMemoryCache();
        var cache = new ProbeHookCache(inner);
        var provider = BuildProviderWith(cache);
        var filter = new IdempotencyEndpointFilter();
        var id = Guid.NewGuid();

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // The original is in flight and holds the reservation, so the duplicate below is refused.
        var holderCall = filter.InvokeAsync(
            new TestFilterContext(NewContext(provider, new MemoryStream())),
            async _ =>
            {
                started.SetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                return TypedResults.Ok(new SampleDto(id, "original"));
            }).AsTask();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // The original stores its response between the duplicate's first probe and its refusal.
        cache.SeedOnNextProbe(key => SeedEntryAsync(inner, key, id));

        int executions = 0;
        var duplicate = NewContext(provider, new MemoryStream());
        await filter.InvokeAsync(
            new TestFilterContext(duplicate),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "duplicate")));
            });

        release.SetResult();
        await holderCall.WaitAsync(TimeSpan.FromSeconds(10));

        executions.ShouldBe(0);
        duplicate.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeTrue(
            "409ing straight from the refusal returns a conflict for a request whose answer is already " +
            "stored — the refused duplicate has to re-probe once before reporting the conflict.");
    }

    // ─── the header allow-list is a list, not a copy ───────────────────────────────────

    [Fact]
    public async Task Replay_Should_CarryOnlyAllowListedHeaders_When_TheHandlerSetOthers()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();

        var first = NewContext(provider, new MemoryStream());
        await filter.InvokeAsync(
            new TestFilterContext(first),
            invocation =>
            {
                invocation.HttpContext.Response.Headers.ETag = "\"v1\"";
                invocation.HttpContext.Response.Headers.SetCookie = "session=abc; Path=/";
                invocation.HttpContext.Response.Headers["X-Trace"] = "first-call-only";
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget")));
            });

        first.Response.Headers.SetCookie.ToString().ShouldNotBeEmpty("sanity: the first call sets the header on the real response");

        var second = NewContext(provider, new MemoryStream());
        await filter.InvokeAsync(
            new TestFilterContext(second),
            _ => throw new InvalidOperationException("handler must NOT run on an idempotent replay"));

        second.Response.Headers.ETag.ToString().ShouldBe("\"v1\"", "ETag is on the allow-list and carries meaning for the caller");
        second.Response.Headers.SetCookie.ToString().ShouldBeEmpty(
            "replaying everything the first response carried resurrects a stale Set-Cookie (and a stale " +
            "Content-Length or Date, which corrupts the response) hours after the fact.");
        second.Response.Headers.ContainsKey("X-Trace").ShouldBeFalse();
    }

    // ─── a store that fails must not fail the request ──────────────────────────────────

    [Fact]
    public async Task Filter_Should_Succeed_When_TheStoreFails()
    {
        var provider = BuildProviderWith(new WriteFaultyCache(NewMemoryCache()));
        var filter = new IdempotencyEndpointFilter();

        var result = await filter.InvokeAsync(
            new TestFilterContext(NewContext(provider, new MemoryStream())),
            _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget"))));

        result.ShouldBeOfType<EmptyHttpResult>(
            "the handler's side effect has already committed when the store runs; 500ing the request " +
            "because the cache is down reports a failure for work that succeeded.");
    }

    [Fact]
    public async Task Filter_Should_NotThrow_When_TheReleaseFaults()
    {
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(Task.FromResult(true));
        db.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromException<RedisResult>(new RedisException("release blip")));
        var provider = BuildProvider(new IdempotencyOptions(), RedisMultiplexer(db));
        var filter = new IdempotencyEndpointFilter();

        var result = await filter.InvokeAsync(
            new TestFilterContext(NewContext(provider, new MemoryStream())),
            _ => ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget"))));

        result.ShouldBeOfType<EmptyHttpResult>(
            "the release runs in a finally, after the response has gone to the client — an exception out " +
            "of it can only reset the connection on a request that already succeeded. The short " +
            "ReservationTtl cleans up the missed delete.");
    }

    // ─── the opt-in is per request: no header, no idempotency ──────────────────────────

    [Fact]
    public async Task Filter_Should_PassThrough_When_NoKeyHeaderIsSent()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();
        var handlerResult = TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget"));

        var first = NewContextForKey(provider, idempotencyKey: string.Empty);
        var result = await filter.InvokeAsync(
            new TestFilterContext(first),
            _ => ValueTask.FromResult<object?>(handlerResult));

        result.ShouldBeSameAs(
            handlerResult,
            "with no key the filter is a no-op: the handler's own result goes back unexecuted, exactly as " +
            "on an endpoint without .WithIdempotency().");
        first.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeFalse();

        int executions = 0;
        var second = NewContextForKey(provider, idempotencyKey: string.Empty);
        await filter.InvokeAsync(
            new TestFilterContext(second),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(handlerResult);
            });

        executions.ShouldBe(
            1,
            "keyless requests must not share one entry: dropping the empty-key guard puts every one of " +
            "them in the same bucket and the second caller replays the first caller's response.");
    }

    [Fact]
    public async Task Filter_Should_Reject_When_TheKeyIsLongerThanMaxKeyLength()
    {
        var options = new IdempotencyOptions { MaxKeyLength = 16 };
        var provider = BuildProvider(options, multiplexer: null);
        var filter = new IdempotencyEndpointFilter();

        int executions = 0;
        var result = await filter.InvokeAsync(
            new TestFilterContext(NewContextForKey(provider, new string('k', options.MaxKeyLength + 1))),
            _ =>
            {
                executions++;
                return ValueTask.FromResult<object?>(TypedResults.Ok(new SampleDto(Guid.NewGuid(), "widget")));
            });

        executions.ShouldBe(0, "an over-long key is rejected before the handler runs");
        result.ShouldBeAssignableTo<IStatusCodeHttpResult>()!
            .StatusCode.ShouldBe(
                StatusCodes.Status400BadRequest,
                "an unbounded key is a cache-key injection surface, and the rejection goes out as RFC 9457 " +
                "ProblemDetails like every other error on these endpoints.");
    }

    // ─── an entry written before headers were captured still replays ───────────────────

    [Fact]
    public async Task Replay_Should_Work_When_TheStoredEntryHasNoHeaders()
    {
        var provider = BuildProvider();
        var filter = new IdempotencyEndpointFilter();
        var cache = provider.GetRequiredService<IDistributedCache>();

        // The shape a previous version wrote: status, content type and body, no headers member.
        await cache.SetAsync(
            CacheKeys.IdempotencyEntry("global", $"anon:POST::{Key}"),
            """{"statusCode":200,"contentType":"application/json","body":"eyJvayI6dHJ1ZX0="}"""u8.ToArray(),
            new DistributedCacheEntryOptions());

        var replayBody = new MemoryStream();
        var context = NewContext(provider, replayBody);
        await filter.InvokeAsync(
            new TestFilterContext(context),
            _ => throw new InvalidOperationException("handler must NOT run: the entry is readable"));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        context.Response.Headers.ContainsKey("Idempotency-Replayed").ShouldBeTrue(
            "a rolling deploy replays entries written by the previous version: a missing headers member " +
            "must deserialize to empty, not throw and discard the entry.");
        Encoding.UTF8.GetString(replayBody.ToArray()).ShouldBe("""{"ok":true}""");
    }

    // ─── harness ─────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider() => BuildProvider(new IdempotencyOptions(), multiplexer: null);

    private static ServiceProvider BuildProvider(IdempotencyOptions options, IConnectionMultiplexer? multiplexer)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddSingleton<IOptions<IdempotencyOptions>>(Options.Create(options));
        if (multiplexer is not null)
        {
            services.AddSingleton(multiplexer);
        }

        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildProviderWith(IDistributedCache cache) =>
        BuildProviderWith(cache, tenantAccessor: null, multiplexer: null);

    private static ServiceProvider BuildProviderWith(IDistributedCache cache, IMultiTenantContextAccessor<AppTenantInfo>? tenantAccessor) =>
        BuildProviderWith(cache, tenantAccessor, multiplexer: null);

    private static ServiceProvider BuildProviderWith(IDistributedCache cache, IConnectionMultiplexer multiplexer) =>
        BuildProviderWith(cache, tenantAccessor: null, multiplexer);

    private static ServiceProvider BuildProviderWith(
        IDistributedCache cache,
        IMultiTenantContextAccessor<AppTenantInfo>? tenantAccessor,
        IConnectionMultiplexer? multiplexer)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(cache);
        services.AddSingleton<IOptions<IdempotencyOptions>>(Options.Create(new IdempotencyOptions()));
        if (tenantAccessor is not null)
        {
            services.AddSingleton(tenantAccessor);
        }

        if (multiplexer is not null)
        {
            services.AddSingleton(multiplexer);
        }

        return services.BuildServiceProvider();
    }

    private static MemoryDistributedCache NewMemoryCache() =>
        new(Options.Create(new MemoryDistributedCacheOptions()));

    // The entry the original request would have stored, written straight into the store.
    private static Task SeedEntryAsync(MemoryDistributedCache cache, string cacheKey, Guid id) =>
        cache.SetAsync(
            cacheKey,
            JsonSerializer.SerializeToUtf8Bytes(
                new CachedIdempotentResponse
                {
                    StatusCode = StatusCodes.Status200OK,
                    ContentType = "application/json",
                    Body = JsonSerializer.SerializeToUtf8Bytes(new SampleDto(id, "original"), CacheJsonOpts),
                },
                CacheJsonOpts),
            new DistributedCacheEntryOptions());

    // What Finbuckle leaves behind for the endpoint filter: the tenant the request is scoped to,
    // which for a root operator using the tenant header is the target, not the caller's own tenant.
    private static IMultiTenantContextAccessor<AppTenantInfo> TenantContext(string tenantId)
    {
        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(new AppTenantInfo(tenantId, tenantId)));
        return accessor;
    }

    // A Redis stand-in whose reservation commands hit the same dictionary the entries live in, which
    // is how the two sit in a real deployment.
    private static IConnectionMultiplexer KeyspaceMultiplexer(SharedKeyspace keyspace)
    {
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(ci => Task.FromResult(
                ci.ArgAt<When>(3) == When.NotExists
                    ? keyspace.TryAdd(ci.ArgAt<RedisKey>(0).ToString(), (byte[])ci.ArgAt<RedisValue>(1)!)
                    : keyspace.Set(ci.ArgAt<RedisKey>(0).ToString(), (byte[])ci.ArgAt<RedisValue>(1)!)));
        db.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(ci =>
            {
                keyspace.RemoveIfValueMatches(
                    ci.ArgAt<RedisKey[]>(1)[0].ToString(),
                    (byte[])ci.ArgAt<RedisValue[]>(2)[0]!);
                return Task.FromResult(RedisResult.Create(1));
            });
        return RedisMultiplexer(db);
    }

    private static IConnectionMultiplexer RedisMultiplexer(IDatabase db)
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        mux.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return mux;
    }

    private static RouteEndpoint RouteEndpointFor(string pattern) => new(
        _ => Task.CompletedTask,
        RoutePatternFactory.Parse(pattern),
        order: 0,
        new EndpointMetadataCollection(),
        displayName: pattern);

    private DefaultHttpContext NewContext(IServiceProvider provider, Stream? responseBody = null) =>
        NewContextForKey(provider, Key, responseBody);

    private static DefaultHttpContext NewContextForKey(IServiceProvider provider, string idempotencyKey) =>
        NewContextForKey(provider, idempotencyKey, new MemoryStream());

    private static DefaultHttpContext NewContextForKey(IServiceProvider provider, string idempotencyKey, Stream? responseBody)
    {
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = "POST";
        context.Request.Headers["Idempotency-Key"] = idempotencyKey;
        if (responseBody is not null)
        {
            context.Response.Body = responseBody;
        }

        return context;
    }

    // A context whose response reports itself as already started, which is what a handler that writes
    // to HttpContext.Response leaves behind on a real server.
    private DefaultHttpContext NewStartedResponseContext(IServiceProvider provider, Stream responseBody)
    {
        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature
        {
            Method = "POST",
            Path = "/",
            Headers = new HeaderDictionary { ["Idempotency-Key"] = Key },
        });
        features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(responseBody));

        return new DefaultHttpContext(features) { RequestServices = provider };
    }

    private static ClaimsPrincipal TenantPrincipal(string tenantId) =>
        new(new ClaimsIdentity([new Claim(ClaimConstants.Tenant, tenantId)], "test"));

    private static ClaimsPrincipal CallerPrincipal(string tenantId, string userId) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimConstants.Tenant, tenantId), new Claim(ClaimTypes.NameIdentifier, userId)],
            "test"));

    private sealed record SampleDto(Guid Id, string Name);

    /// <summary>
    /// Wraps the in-memory cache so a test can see the <see cref="CancellationToken"/> the filter
    /// stores with. <c>MemoryDistributedCache</c> ignores it entirely, so without this the difference
    /// between <c>CancellationToken.None</c> and a cancelled <c>RequestAborted</c> — the whole point
    /// of the store-outlives-the-request fix — is invisible to every assertion.
    /// </summary>
    private sealed class TokenSensitiveCache(IDistributedCache inner) : IDistributedCache
    {
        public byte[]? Get(string key) => inner.Get(key);

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => inner.GetAsync(key, token);

        public void Refresh(string key) => inner.Refresh(key);

        public Task RefreshAsync(string key, CancellationToken token = default) => inner.RefreshAsync(key, token);

        public void Remove(string key) => inner.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default) => inner.RemoveAsync(key, token);

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => inner.Set(key, value, options);

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            // What a real IDistributedCache does: RedisCache.SetImplAsync calls ThrowIfCancellationRequested.
            token.ThrowIfCancellationRequested();
            return inner.SetAsync(key, value, options, token);
        }
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted => true;

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public string? ReasonPhrase { get; set; }

        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }
    }

    /// <summary>
    /// A cache whose connection is down: every operation throws, the way `RedisCache` does when the
    /// server is unreachable.
    /// </summary>
    private sealed class FaultyCache : IDistributedCache
    {
        public byte[]? Get(string key) => throw new InvalidOperationException("cache is down");

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromException<byte[]?>(new InvalidOperationException("cache is down"));

        public void Refresh(string key) => throw new InvalidOperationException("cache is down");

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            Task.FromException(new InvalidOperationException("cache is down"));

        public void Remove(string key) => throw new InvalidOperationException("cache is down");

        public Task RemoveAsync(string key, CancellationToken token = default) =>
            Task.FromException(new InvalidOperationException("cache is down"));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            throw new InvalidOperationException("cache is down");

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
            Task.FromException(new InvalidOperationException("cache is down"));
    }

    /// <summary>
    /// One keyspace for both the cached entries and the reservations — what a real Redis is.
    /// </summary>
    private sealed class SharedKeyspace
    {
        private readonly Dictionary<string, byte[]> _values = new(StringComparer.Ordinal);

        public bool TryAdd(string key, byte[] value)
        {
            lock (_values)
            {
                return _values.TryAdd(key, value);
            }
        }

        public bool Set(string key, byte[] value)
        {
            lock (_values)
            {
                _values[key] = value;
                return true;
            }
        }

        public byte[]? Get(string key)
        {
            lock (_values)
            {
                return _values.TryGetValue(key, out var value) ? value : null;
            }
        }

        public void Remove(string key)
        {
            lock (_values)
            {
                _values.Remove(key);
            }
        }

        public void RemoveIfValueMatches(string key, byte[] expected)
        {
            lock (_values)
            {
                if (_values.TryGetValue(key, out var value) && value.AsSpan().SequenceEqual(expected))
                {
                    _values.Remove(key);
                }
            }
        }
    }

    private sealed class KeyspaceCache(SharedKeyspace keyspace) : IDistributedCache
    {
        public byte[]? Get(string key) => keyspace.Get(key);

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(keyspace.Get(key));

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => keyspace.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            keyspace.Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => keyspace.Set(key, value);

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            keyspace.Set(key, value);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Lets a test drop an entry into the store at an exact point in the filter's sequence: the hook
    /// runs after one probe has already answered, which is the window in which the original request
    /// stores its response and releases the lock.
    /// </summary>
    private sealed class ProbeHookCache(IDistributedCache inner) : IDistributedCache
    {
        private Func<string, Task>? _onNextProbe;

        public void SeedOnNextProbe(Func<string, Task> seed) => Interlocked.Exchange(ref _onNextProbe, seed);

        public byte[]? Get(string key) => inner.Get(key);

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            var bytes = await inner.GetAsync(key, token).ConfigureAwait(false);
            var hook = Interlocked.Exchange(ref _onNextProbe, null);
            if (hook is not null)
            {
                await hook(key).ConfigureAwait(false);
            }

            return bytes;
        }

        public void Refresh(string key) => inner.Refresh(key);

        public Task RefreshAsync(string key, CancellationToken token = default) => inner.RefreshAsync(key, token);

        public void Remove(string key) => inner.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default) => inner.RemoveAsync(key, token);

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => inner.Set(key, value, options);

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
            inner.SetAsync(key, value, options, token);
    }

    /// <summary>
    /// Reads fine, refuses to write — a cache that has gone down between the probe and the store.
    /// </summary>
    private sealed class WriteFaultyCache(IDistributedCache inner) : IDistributedCache
    {
        public byte[]? Get(string key) => inner.Get(key);

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => inner.GetAsync(key, token);

        public void Refresh(string key) => inner.Refresh(key);

        public Task RefreshAsync(string key, CancellationToken token = default) => inner.RefreshAsync(key, token);

        public void Remove(string key) => inner.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default) => inner.RemoveAsync(key, token);

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            throw new InvalidOperationException("cache write is down");

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
            Task.FromException(new InvalidOperationException("cache write is down"));
    }

    private sealed class TestFilterContext : EndpointFilterInvocationContext
    {
        public TestFilterContext(HttpContext httpContext) => HttpContext = httpContext;

        public override HttpContext HttpContext { get; }

        public override IList<object?> Arguments { get; } = new List<object?>();

        public override T GetArgument<T>(int index) => (T)Arguments[index]!;
    }
}
