using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace FSH.Framework.Infrastructure.Logging.Serilog;

public static class Extensions
{
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Host.UseSerilog((context, logger) =>
        {
            logger.WriteTo.OpenTelemetry(options =>
            {
                try
                {
                    options.Endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                    var headers = builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"]?.Split(',') ?? [];
                    foreach (var header in headers)
                    {
                        var parts = header.Split('=');
                        if (parts.Length != 2)
                        {
                            throw new ArgumentException($"Invalid header format: {header}. Expected format: 'key=value'");
                        }
                        options.Headers.Add(parts[0], parts[1]);
                    }
                    options.ResourceAttributes.Add("service.name", "apiservice");
                    
                    var resourceAttributeConfig = builder.Configuration["OTEL_RESOURCE_ATTRIBUTES"];
                    if (!string.IsNullOrEmpty(resourceAttributeConfig))
                    {
                        var attributeParts = resourceAttributeConfig.Split('=');
                        if (attributeParts.Length == 2)
                        {
                            options.ResourceAttributes.Add(attributeParts[0], attributeParts[1]);
                        }
                    }
                }
                catch
                {
                    // Ignore OpenTelemetry configuration errors
                }
            });
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
