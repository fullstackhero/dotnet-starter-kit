using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FSH.Framework.Caching.Telemetry;

/// <summary>
/// OpenTelemetry primitives for the caching building block.
/// Exposed as static fields so they can be wired into the OTel pipeline via
/// <c>metrics.AddMeter(CachingTelemetry.MeterName)</c> and
/// <c>tracing.AddSource(CachingTelemetry.ActivitySourceName)</c>.
/// </summary>
public static class CachingTelemetry
{
    /// <summary>Name of the <see cref="ActivitySource"/> used for cache spans.</summary>
    public const string ActivitySourceName = "FSH.Caching";

    /// <summary>Name of the <see cref="Meter"/> used for cache metrics.</summary>
    public const string MeterName = "FSH.Caching";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    internal static readonly Meter Meter = new(MeterName);

    /// <summary>L1 or L2 cache hit — factory was not invoked.</summary>
    internal static readonly Counter<long> Hits = Meter.CreateCounter<long>(
        "fsh.cache.hits",
        unit: "{hit}",
        description: "Number of cache reads that returned without invoking the factory.");

    /// <summary>Cache miss — factory was invoked to produce a fresh value.</summary>
    internal static readonly Counter<long> Misses = Meter.CreateCounter<long>(
        "fsh.cache.misses",
        unit: "{miss}",
        description: "Number of cache reads that invoked the factory because the entry was missing or invalidated.");

    /// <summary>Explicit removals — <c>RemoveAsync</c> or <c>RemoveByTagAsync</c>.</summary>
    internal static readonly Counter<long> Invalidations = Meter.CreateCounter<long>(
        "fsh.cache.invalidations",
        unit: "{invalidation}",
        description: "Number of explicit cache removals (RemoveAsync + RemoveByTagAsync).");

    /// <summary>Factory execution duration — only recorded on a cache miss.</summary>
    internal static readonly Histogram<double> FactoryDurationMs = Meter.CreateHistogram<double>(
        "fsh.cache.factory.duration",
        unit: "ms",
        description: "Duration of the factory invocation on a cache miss.");
}
