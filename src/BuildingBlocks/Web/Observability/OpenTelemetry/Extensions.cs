using FSH.Framework.Caching.Telemetry;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using static FSH.Framework.Web.Observability.OpenTelemetry.OpenTelemetryOptions;

namespace FSH.Framework.Web.Observability.OpenTelemetry;

public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    public static IHostApplicationBuilder AddHeroOpenTelemetry(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new OpenTelemetryOptions();
        builder.Configuration.GetSection(OpenTelemetryOptions.SectionName).Bind(options);

        if (!options.Enabled)
        {
            return builder;
        }

        builder.Services.AddOptions<OpenTelemetryOptions>()
            .BindConfiguration(OpenTelemetryOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Honor the orchestrator's identity: Aspire (and any OTLP collector) injects OTEL_SERVICE_NAME as the
        // resource name it knows the process by (e.g. "fsh-starter-api"). Overriding it with the entry-assembly
        // name ("FSH.Starter.Api") de-correlates our telemetry from that resource, so the dashboard lists the
        // process twice. Adopt the injected name when present; fall back to ApplicationName when running standalone.
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
            ?? builder.Environment.ApplicationName;

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(serviceName: serviceName);

        // Shared ActivitySource for spans (Mediator, etc.)
        builder.Services.AddSingleton(new ActivitySource(builder.Environment.ApplicationName));

        // Aspire (and any OTLP collector) injects OTEL_EXPORTER_OTLP_ENDPOINT into the process. When present we
        // export to it even if Exporter.Otlp.Enabled is false in config, and we let the OpenTelemetry SDK read the
        // endpoint/protocol from the standard OTEL_EXPORTER_OTLP_* env vars instead of overriding with config — that
        // is how telemetry reaches the Aspire dashboard's Traces/Metrics tabs (its OTLP receiver is on a dynamic port).
        var useEnvEndpoint = !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

        ConfigureMetricsAndTracing(builder, options, resourceBuilder, serviceName, useEnvEndpoint);

        return builder;
    }

    private static void ConfigureMetricsAndTracing(
        IHostApplicationBuilder builder,
        OpenTelemetryOptions options,
        ResourceBuilder resourceBuilder,
        string serviceName,
        bool useEnvEndpoint)
    {
        var exportOtlp = options.Exporter.Otlp.Enabled || useEnvEndpoint;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(serviceName))
            .WithMetrics(metrics =>
            {
                if (!options.Metrics.Enabled)
                {
                    return;
                }

                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddNpgsqlInstrumentation()
                    .AddRuntimeInstrumentation();

                // Apply histogram buckets for HTTP server duration
                if (options.Http.Histograms.Enabled)
                {
                    metrics.AddView(
                        "http.server.duration",
                        new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = GetHistogramBuckets(options)
                        });
                }

                // Caching building block metrics (hits, misses, factory duration, invalidations).
                metrics.AddMeter(CachingTelemetry.MeterName);

                // Auditing pipeline metrics (published, dropped, flush, dead-letter).
                metrics.AddMeter("FSH.Modules.Auditing");

                foreach (var meterName in options.Metrics.MeterNames ?? Array.Empty<string>())
                {
                    metrics.AddMeter(meterName);
                }

                if (exportOtlp)
                {
                    metrics.AddOtlpExporter(otlp =>
                    {
                        ConfigureOtlpExporter(options.Exporter.Otlp, otlp, useEnvEndpoint);
                    });
                }
            })
            .WithTracing(tracing =>
            {
                if (!options.Tracing.Enabled)
                {
                    return;
                }

                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(instrumentation =>
                    {
                        instrumentation.Filter = context => !IsHealthCheck(context.Request.Path);
                        instrumentation.EnrichWithHttpRequest = EnrichWithHttpRequest;
                        instrumentation.EnrichWithHttpResponse = EnrichWithHttpResponse;
                    })
                    .AddHttpClientInstrumentation()
                    .AddNpgsql()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddRedisInstrumentation(redis =>
                    {
                        if (options.Data.FilterRedisCommands)
                        {
                            redis.SetVerboseDatabaseStatements = false;
                        }
                    })
                    .AddSource(builder.Environment.ApplicationName)
                    .AddSource("FSH.Hangfire")
                    .AddSource(CachingTelemetry.ActivitySourceName);

                if (exportOtlp)
                {
                    tracing.AddOtlpExporter(otlp =>
                    {
                        ConfigureOtlpExporter(options.Exporter.Otlp, otlp, useEnvEndpoint);
                    });
                }
            });

        // Mediator spans (optional): add behavior in DI for pipeline spans.
        if (options.Mediator.Enabled)
        {
            builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MediatorTracingBehavior<,>));
        }

        // Hangfire/job instrumentation placeholder: currently enabled via Jobs.Enabled; wire hooks in jobs building block.
    }

    private static double[] GetHistogramBuckets(OpenTelemetryOptions options)
    {
        if (options.Http.Histograms.BucketBoundaries is { Length: > 0 } custom)
        {
            return custom;
        }

        // Default buckets in seconds (fast to slow)
        return new[] { 0.01, 0.05, 0.1, 0.25, 0.5, 1, 2, 5 };
    }

    private static bool IsHealthCheck(PathString path) =>
        path.StartsWithSegments(HealthEndpointPath) ||
        path.StartsWithSegments(AlivenessEndpointPath);

    private static void EnrichWithHttpRequest(Activity activity, HttpRequest request)
    {
        activity.SetTag("http.method", request.Method);
        activity.SetTag("http.scheme", request.Scheme);
        activity.SetTag("http.host", request.Host.Value);
        activity.SetTag("http.target", request.Path);
    }

    private static void EnrichWithHttpResponse(Activity activity, HttpResponse response)
    {
        activity.SetTag("http.status_code", response.StatusCode);
    }

    private static void ConfigureOtlpExporter(
        OtlpOptions options,
        OtlpExporterOptions otlp,
        bool useEnvEndpoint)
    {
        // When an OTLP endpoint is supplied via OTEL_EXPORTER_OTLP_ENDPOINT (e.g. Aspire), defer entirely to the
        // SDK's env-var resolution for endpoint + protocol; overriding here would point us away from the dashboard.
        if (useEnvEndpoint)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            otlp.Endpoint = new Uri(options.Endpoint);
        }

        var protocol = options.Protocol?.Trim().ToLowerInvariant();
        otlp.Protocol = protocol switch
        {
            "grpc" => OtlpExportProtocol.Grpc,
            "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
            _ => otlp.Protocol
        };
    }
}