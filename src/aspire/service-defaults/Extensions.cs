using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FSH.Starter.Aspire.ServiceDefaults;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        #region OpenTelemetry

        // Configure OpenTelemetry service resource details
        // See https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions
        var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
        var entryAssemblyName = entryAssembly?.GetName();
        var versionAttribute = entryAssembly?.GetCustomAttributes(false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault();
        var resourceServiceName = entryAssemblyName?.Name;
        var resourceServiceVersion = versionAttribute?.InformationalVersion ?? entryAssemblyName?.Version?.ToString();
        var attributes = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["host.name"] = Environment.MachineName,
            ["service.names"] = "FSH.Starter.WebApi.Host",
            ["os.description"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            ["deployment.environment"] = builder.Environment.EnvironmentName.ToUpperInvariant()
        };
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: resourceServiceName ?? "Unknown", serviceVersion: resourceServiceVersion)
            .AddTelemetrySdk()
            //.AddEnvironmentVariableDetector()
            .AddAttributes(attributes);

        #endregion region


        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.SetResourceBuilder(resourceBuilder);
            // No OTLP exporter for logging in this context
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation();
                var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint!);
                    });
                }
                metrics.AddPrometheusExporter(options =>
                {
                    options.DisableTotalNameSuffixForCounters = true;
                });
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing.SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(nci => nci.RecordException = true)
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();
                var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint!);
                    });
                }
            });

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // The following line enables the Prometheus endpoint (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
        app.UseOpenTelemetryPrometheusScrapingEndpoint(
            context =>
            {
                if (context.Request.Path != "/metrics") return false;
                return true;
            });

        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks("/health").AllowAnonymous();
        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") })
            .AllowAnonymous();

        return app;
    }
}
