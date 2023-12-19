using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Filters;

namespace FSH.Framework.Infrastructure.Logging.Serilog;

public static class Extensions
{
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOptions<LogConfig>().BindConfiguration(nameof(LogConfig));
        _ = builder.Host.UseSerilog((_, sp, logger) =>
        {
            var settings = sp.GetRequiredService<IOptions<LogConfig>>().Value;
            logger.ConfigureEnrichers(settings.AppName);
            logger.ConfigureSinks(settings);
            logger.SetMinimumLogLevel(settings.MinimumLogLevel);
            logger.OverideMinimumLogLevel();

        });
        return builder;
    }

    private static void ConfigureSinks(this LoggerConfiguration logger, LogConfig config)
    {
        if (config.WriteToConsole)
        {
            logger.WriteTo.Async(wt => wt.Console());
        }
    }

    private static void SetMinimumLogLevel(this LoggerConfiguration logger, string minLogLevel)
    {
        switch (minLogLevel.ToUpperInvariant())
        {
            case "DEBUG":
                logger.MinimumLevel.Debug();
                break;
            case "INFORMATION":
                logger.MinimumLevel.Information();
                break;
            case "WARNING":
                logger.MinimumLevel.Warning();
                break;
            default:
                logger.MinimumLevel.Information();
                break;
        }
    }
    private static void OverideMinimumLogLevel(this LoggerConfiguration logger)
    {
        logger
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware"));  //since we have a global exception handler
    }
    private static void ConfigureEnrichers(this LoggerConfiguration logger, string appName)
    {
        logger
            .Enrich.FromLogContext()
            .Enrich.WithProperty("App", appName)
            .Enrich.WithEnvironmentName()
            .Enrich.WithExceptionDetails()
            .Enrich.WithMachineName()
            .Enrich.FromLogContext();
    }
}
