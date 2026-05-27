using System.ComponentModel.DataAnnotations;

namespace FSH.Framework.Web.Observability.OpenTelemetry;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetryOptions";

    /// <summary>
    /// Global switch to turn OpenTelemetry on/off.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public TracingOptions Tracing { get; set; } = new();

    public MetricsOptions Metrics { get; set; } = new();

    public ExporterOptions Exporter { get; set; } = new();

    /// <summary>
    /// Job instrumentation options (e.g., Hangfire).
    /// </summary>
    public JobOptions Jobs { get; set; } = new();

    /// <summary>
    /// Mediator pipeline instrumentation options.
    /// </summary>
    public MediatorOptions Mediator { get; set; } = new();

    /// <summary>
    /// HTTP instrumentation options (including histograms).
    /// </summary>
    public HttpOptions Http { get; set; } = new();

    /// <summary>
    /// EF/Redis instrumentation filtering options.
    /// </summary>
    public DataOptions Data { get; set; } = new();

    public sealed class TracingOptions
    {
        public bool Enabled { get; set; } = true;
    }

    public sealed class MetricsOptions
    {
        public bool Enabled { get; set; } = true;
        public string[]? MeterNames { get; set; }
    }

    public sealed class ExporterOptions
    {
        public OtlpOptions Otlp { get; set; } = new();
    }

    public sealed class OtlpOptions
    {
        public bool Enabled { get; set; } = true;

        [Url]
        public string? Endpoint { get; set; }

        /// <summary>
        /// Transport protocol, e.g. "grpc" or "http/protobuf".
        /// </summary>
        public string? Protocol { get; set; }
    }

    public sealed class JobOptions
    {
        /// <summary>Enable tracing/metrics for jobs (e.g., Hangfire).</summary>
        public bool Enabled { get; set; } = true;
    }

    public sealed class MediatorOptions
    {
        /// <summary>Enable spans around Mediator commands/queries.</summary>
        public bool Enabled { get; set; } = true;
    }

    public sealed class HttpOptions
    {
        public HistogramOptions Histograms { get; set; } = new();

        public sealed class HistogramOptions
        {
            /// <summary>Enable HTTP request duration histograms.</summary>
            public bool Enabled { get; set; } = true;

            /// <summary>Custom bucket boundaries (seconds). If null/empty, defaults apply.</summary>
            public double[]? BucketBoundaries { get; set; }
        }
    }

    public sealed class DataOptions
    {
        /// <summary>Suppress SQL text in EF instrumentation to reduce PII/noise.</summary>
        public bool FilterEfStatements { get; set; } = true;

        /// <summary>Suppress Redis command text in instrumentation to reduce noise.</summary>
        public bool FilterRedisCommands { get; set; } = true;
    }

}