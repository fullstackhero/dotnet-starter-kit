using FSH.Framework.Core.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace FSH.Framework.Infrastructure.Observability;
internal static class Extensions
{
    internal static WebApplicationBuilder ConfigureObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(serviceName: MetricsConstants.AppName))
        .WithMetrics(metrics =>
        {
            metrics
            .AddMeter(MetricsConstants.AppName)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri(builder.Configuration["OpenTelemetryOptions:Endpoint"]!);
            });
        });
        return builder;
    }
}
