using Serilog;
using Serilog.Core;

namespace FSH.Framework.Web.Observability.Logging.Serilog;

public static class StaticLogger
{
    public static void EnsureInitialized()
    {
        if (Log.Logger is not Logger)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
        }
    }
}