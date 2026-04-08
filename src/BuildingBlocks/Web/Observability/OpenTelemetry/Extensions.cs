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

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(serviceName: builder.Environment.ApplicationName);

        // Shared ActivitySource for spans (Mediator, etc.)
        builder.Services.AddSingleton(new ActivitySource(builder.Environment.ApplicationName));

        ConfigureMetricsAndTracing(builder, options, resourceBuilder);

        return builder;
    }

    private static void ConfigureMetricsAndTracing(
        IHostApplicationBuilder builder,
        OpenTelemetryOptions options,
        ResourceBuilder resourceBuilder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(builder.Environment.ApplicationName))
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

                foreach (var meterName in options.Metrics.MeterNames ?? Array.Empty<string>())
                {
                    metrics.AddMeter(meterName);
                }

                if (options.Exporter.Otlp.Enabled)
                {
                    metrics.AddOtlpExporter(otlp =>
                    {
                        ConfigureOtlpExporter(options.Exporter.Otlp, otlp);
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

                if (options.Exporter.Otlp.Enabled)
                {
                    tracing.AddOtlpExporter(otlp =>
                    {
                        ConfigureOtlpExporter(options.Exporter.Otlp, otlp);
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
        OtlpExporterOptions otlp)
    {
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