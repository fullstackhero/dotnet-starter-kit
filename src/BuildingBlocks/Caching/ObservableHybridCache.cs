using System.Diagnostics;
using FSH.Framework.Caching.Telemetry;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Framework.Caching;

/// <summary>
/// <see cref="HybridCache"/> decorator that records OpenTelemetry metrics and spans for every
/// operation. The inner cache is the regular <see cref="HybridCache"/> registered by
/// <c>AddHybridCache</c>; this type wraps it transparently so consumers inject <c>HybridCache</c>
/// as usual and get observability for free.
/// </summary>
/// <remarks>
/// Hit/miss attribution is done by wrapping the factory delegate with a flag and recording
/// based on whether the flag flipped. L1 and L2 hits are both counted as "hit" — HybridCache
/// does not surface which layer served the read. Factory duration is recorded only on a miss.
/// </remarks>
internal sealed class ObservableHybridCache : HybridCache
{
    private readonly HybridCache _inner;

    public ObservableHybridCache(HybridCache inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public override async ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        using var activity = CachingTelemetry.ActivitySource.StartActivity(
            "cache.get_or_create",
            ActivityKind.Internal);
        activity?.SetTag("cache.system", "fsh.hybrid");
        activity?.SetTag("cache.key", key);

        // Wrap the factory so we can record hit/miss and factory duration without allocating a
        // closure over caller state — the caller's state flows through the TState parameter.
        var wrappedState = new FactoryWrapperState<TState, T>(state, factory, Invoked: false);
        var wrapperBox = new StrongBox<FactoryWrapperState<TState, T>>(wrappedState);

        T result;
        try
        {
            result = await _inner.GetOrCreateAsync(
                key,
                wrapperBox,
                static async (box, ct) =>
                {
                    var sw = ValueStopwatch.StartNew();
                    box.Value.Invoked = true;
                    try
                    {
                        return await box.Value.Factory(box.Value.State, ct).ConfigureAwait(false);
                    }
                    finally
                    {
                        CachingTelemetry.FactoryDurationMs.Record(sw.ElapsedMilliseconds);
                    }
                },
                options,
                tags,
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }

        if (wrapperBox.Value.Invoked)
        {
            CachingTelemetry.Misses.Add(1);
            activity?.SetTag("cache.hit", false);
        }
        else
        {
            CachingTelemetry.Hits.Add(1);
            activity?.SetTag("cache.hit", true);
        }

        return result;
    }

    public override ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = CachingTelemetry.ActivitySource.StartActivity(
            "cache.set",
            ActivityKind.Internal);
        activity?.SetTag("cache.system", "fsh.hybrid");
        activity?.SetTag("cache.key", key);

        return _inner.SetAsync(key, value, options, tags, cancellationToken);
    }

    public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        using var activity = CachingTelemetry.ActivitySource.StartActivity(
            "cache.remove",
            ActivityKind.Internal);
        activity?.SetTag("cache.system", "fsh.hybrid");
        activity?.SetTag("cache.key", key);
        CachingTelemetry.Invalidations.Add(1);

        return _inner.RemoveAsync(key, cancellationToken);
    }

    public override ValueTask RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        CachingTelemetry.Invalidations.Add(1);
        return _inner.RemoveAsync(keys, cancellationToken);
    }

    public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        using var activity = CachingTelemetry.ActivitySource.StartActivity(
            "cache.remove_by_tag",
            ActivityKind.Internal);
        activity?.SetTag("cache.system", "fsh.hybrid");
        activity?.SetTag("cache.tag", tag);
        CachingTelemetry.Invalidations.Add(1);

        return _inner.RemoveByTagAsync(tag, cancellationToken);
    }

    public override ValueTask RemoveByTagAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        CachingTelemetry.Invalidations.Add(1);
        return _inner.RemoveByTagAsync(tags, cancellationToken);
    }

    // State flows through TState to avoid a per-call closure; the StrongBox observes the
    // "factory invoked?" flag after the inner call (HybridCache doesn't surface hit/miss).
    private struct FactoryWrapperState<TState, T>
    {
        public FactoryWrapperState(TState state, Func<TState, CancellationToken, ValueTask<T>> factory, bool Invoked)
        {
            State = state;
            Factory = factory;
            this.Invoked = Invoked;
        }

        public TState State;
        public Func<TState, CancellationToken, ValueTask<T>> Factory;
        public bool Invoked;
    }

    /// <summary>Minimal reference-type box so the struct state can be observed post-call.</summary>
    private sealed class StrongBox<T>
    {
        public T Value;
        public StrongBox(T value) => Value = value;
    }

    /// <summary>Struct-based stopwatch to avoid the per-call <see cref="Stopwatch"/> allocation.</summary>
    private readonly struct ValueStopwatch
    {
        private static readonly double TimestampToMs = 1000.0 / Stopwatch.Frequency;
        private readonly long _start;

        private ValueStopwatch(long start) => _start = start;

        public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());

        public double ElapsedMilliseconds => (Stopwatch.GetTimestamp() - _start) * TimestampToMs;
    }
}
