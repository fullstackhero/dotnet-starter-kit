using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.OpenTelemetry;
using System.Globalization;

namespace FSH.Framework.Infrastructure.Logging.Serilog;

public static class Extensions
{
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Extract configuration values before logger setup
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var otlpHeaders = builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"]?.Split(',') ?? Array.Empty<string>();
        var resourceAttributeConfig = builder.Configuration["OTEL_RESOURCE_ATTRIBUTES"];

        var headersDict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var header in otlpHeaders)
        {
            var parts = header.Split('=');
            if (parts.Length == 2)
            {
                headersDict[parts[0]] = parts[1];
            }
        }

        var resourceAttributes = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            { "service.name", "apiservice" }
        };
        if (!string.IsNullOrEmpty(resourceAttributeConfig))
        {
            var attributeParts = resourceAttributeConfig.Split('=');
            if (attributeParts.Length == 2)
            {
                resourceAttributes[attributeParts[0]] = attributeParts[1];
            }
        }

        var endpoint = string.IsNullOrWhiteSpace(otlpEndpoint) ? "http://localhost:4317/v1/logs" : otlpEndpoint;

        builder.Host.UseSerilog((context, logger) =>
        {
            logger.WriteTo.OpenTelemetry(
                endpoint: endpoint,
                resourceAttributes: resourceAttributes,
                headers: headersDict,
                formatProvider: CultureInfo.InvariantCulture
            );
            logger.ReadFrom.Configuration(context.Configuration);
            logger.Enrich.FromLogContext();
            logger.Enrich.WithCorrelationId();
            logger
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
                .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware"));
        });
        return builder;
    }
}
