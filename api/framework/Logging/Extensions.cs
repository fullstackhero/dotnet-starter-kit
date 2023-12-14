using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace FSH.Framework.Logging;

public static class Extensions
{
    public static WebApplicationBuilder AddFSHLogging(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOptions<LogConfig>().BindConfiguration(nameof(LogConfig));
        _ = builder.Host.UseSerilog((_, sp, serilogConfig) =>
        {
            var logConfig = sp.GetRequiredService<IOptions<LogConfig>>().Value;
            serilogConfig.WriteTo.Async(wt => wt.Console());
            SetMinimumLogLevel(serilogConfig, logConfig.MinimumLogLevel);
            OverideMinimumLogLevel(serilogConfig);

        });
        return builder;
    }
    private static void SetMinimumLogLevel(LoggerConfiguration serilogConfig, string minLogLevel)
    {
        switch (minLogLevel.ToUpperInvariant())
        {
            case "DEBUG":
                serilogConfig.MinimumLevel.Debug();
                break;
            case "INFORMATION":
                serilogConfig.MinimumLevel.Information();
                break;
            case "WARNING":
                serilogConfig.MinimumLevel.Warning();
                break;
            default:
                serilogConfig.MinimumLevel.Information();
                break;
        }
    }
    private static void OverideMinimumLogLevel(LoggerConfiguration serilogConfig)
    {
        serilogConfig
                     .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                     .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
                     .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                     .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error);
    }
}
