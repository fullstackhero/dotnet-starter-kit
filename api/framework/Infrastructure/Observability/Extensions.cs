using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace FSH.Framework.Infrastructure.Observability;
internal static class Extensions
{
    internal static IServiceCollection ConfigureObservability(this IServiceCollection services)
    {
        // Configurer OpenTelemetry pour le traçage et les métriques
        services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(serviceName: "fullstackhero"))
        .WithMetrics(metrics =>
            metrics
            .AddAspNetCoreInstrumentation() // ASP.NET Core related
            .AddRuntimeInstrumentation() // .NET Runtime metrics like - GC, Memory Pressure, Heap Leaks etc
            .AddPrometheusExporter() // Prometheus Exporter
        );
        return services;
    }
}
