using Serilog;

namespace FSH.WebApi.Infrastructure.Common.Extensions;

public static class SerilogExtensions
{
    public static void Refresh(this ILogger logger)
    {
        if(logger != null && logger is not Serilog.Core.Logger) Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Console().CreateLogger();
    }
}