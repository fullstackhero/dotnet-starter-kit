using Serilog;
using Serilog.Core;
using Serilog.OpenTelemetry;
using Serilog.Sinks.OpenTelemetry;

namespace FSH.Framework.Infrastructure.Logging.Serilog;
public static class StaticLogger
{
    public static void EnsureInitialized()
    {
        if (Log.Logger is not Logger)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.OpenTelemetry()
                .CreateLogger();
        }
    }
}
