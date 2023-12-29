using FSH.Framework.Infrastructure;
using FSH.Framework.Infrastructure.Logging.Serilog;
using FSH.WebApi.Server;
using Serilog;

StaticLogger.EnsureInitialized();
Log.Information("server booting up..");
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddFshFramework();
    builder.AddModules();

    var app = builder.Build();
    app.UseFshFramework();
    app.UseModules();
    app.Run();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("HostAbortedException", StringComparison.Ordinal))
{
    StaticLogger.EnsureInitialized();
    Log.Fatal(ex, "unhandled exception");
}
finally
{
    StaticLogger.EnsureInitialized();
    Log.Information("server shutting down..");
    Log.CloseAndFlush();
}
