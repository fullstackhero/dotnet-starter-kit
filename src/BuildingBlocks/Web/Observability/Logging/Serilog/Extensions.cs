using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace FSH.Framework.Web.Observability.Logging.Serilog;

public static class Extensions
{
    public static IHostApplicationBuilder AddHeroLogging(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton<HttpRequestContextEnricher>();
        builder.Services.AddSerilog((context, logger) =>
        {
            var httpEnricher = context.GetRequiredService<HttpRequestContextEnricher>();
            logger.ReadFrom.Configuration(builder.Configuration);
            logger.Enrich.With(httpEnricher);
            logger
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
                .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
                .MinimumLevel.Override("Finbuckle.MultiTenant", LogEventLevel.Warning)
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware"));
        });
        return builder;
    }
}