using System.Diagnostics.Metrics;
using FSH.Framework.Caching;
using FSH.Framework.Caching.Telemetry;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caching.Tests;

/// <summary>Forces sequential execution — these tests subscribe to a global Meter and would
/// otherwise see measurements from siblings running in parallel. The "Collection" suffix is
/// xUnit's required convention for collection definitions.</summary>
[CollectionDefinition("HybridCacheTelemetry", DisableParallelization = true)]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit convention.")]
public sealed class HybridCacheTelemetryCollection;

/// <summary>
/// Behavioral tests for the OTel decorator wrapping HybridCache. We assert that
/// hit/miss counters fire correctly via a <see cref="MeterListener"/> subscribed to the
/// <c>FSH.Caching</c> meter.
/// </summary>
[Collection("HybridCacheTelemetry")]
public sealed class ObservableHybridCacheTests : IDisposable
{
    private readonly MeterListener _listener;
    private long _hits;
    private long _misses;
    private long _invalidations;
    private long _factoryDurationCount;

    public ObservableHybridCacheTests()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == CachingTelemetry.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            switch (instrument.Name)
            {
                case "fsh.cache.hits": Interlocked.Add(ref _hits, measurement); break;
                case "fsh.cache.misses": Interlocked.Add(ref _misses, measurement); break;
                case "fsh.cache.invalidations": Interlocked.Add(ref _invalidations, measurement); break;
            }
        });
        _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "fsh.cache.factory.duration")
            {
                Interlocked.Increment(ref _factoryDurationCount);
            }
        });
        _listener.Start();
    }

    public void Dispose() => _listener.Dispose();

    private static HybridCache CreateCache()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddHeroCaching(config);
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }

    [Fact]
    public async Task First_call_Should_Record_Miss_And_Factory_Duration()
    {
        var cache = CreateCache();

        var result = await cache.GetOrCreateAsync(
            "obs:miss",
            0,
            static (s, ct) => ValueTask.FromResult("fresh")).ConfigureAwait(true);

        result.ShouldBe("fresh");
        _misses.ShouldBe(1);
        _hits.ShouldBe(0);
        _factoryDurationCount.ShouldBe(1);
    }

    [Fact]
    public async Task Second_call_Should_Record_Hit_And_Not_Invoke_Factory()
    {
        var cache = CreateCache();
        await cache.GetOrCreateAsync("obs:hit", 0, static (s, ct) => ValueTask.FromResult("v")).ConfigureAwait(true);

        var invocations = 0;
        var result = await cache.GetOrCreateAsync(
            "obs:hit",
            0,
            (s, ct) =>
            {
                Interlocked.Increment(ref invocations);
                return ValueTask.FromResult("should-not-run");
            }).ConfigureAwait(true);

        result.ShouldBe("v");
        invocations.ShouldBe(0);
        _hits.ShouldBe(1);
        _misses.ShouldBe(1); // from the warmup call
    }

    [Fact]
    public async Task RemoveAsync_Should_Record_Invalidation()
    {
        var cache = CreateCache();
        await cache.SetAsync("obs:rem", "v").ConfigureAwait(true);

        await cache.RemoveAsync("obs:rem").ConfigureAwait(true);

        _invalidations.ShouldBe(1);
    }

    [Fact]
    public async Task RemoveByTagAsync_Should_Record_Invalidation()
    {
        var cache = CreateCache();
        await cache.SetAsync("obs:tagged", "v", tags: ["group"]).ConfigureAwait(true);

        await cache.RemoveByTagAsync("group").ConfigureAwait(true);

        _invalidations.ShouldBe(1);
    }

    [Fact]
    public async Task Decorator_Should_Propagate_Factory_Exception()
    {
        var cache = CreateCache();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await cache.GetOrCreateAsync<int, string>(
                "obs:throw",
                0,
                static (s, ct) => throw new InvalidOperationException("boom"));
        }).ConfigureAwait(true);
    }
}
