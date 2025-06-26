using FSH.Framework.Core;
using FSH.Framework.Infrastructure;
using FSH.Framework.Infrastructure.Logging.Serilog;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Starter.Aspire.ServiceDefaults;
using FSH.Starter.WebApi.Host;
using Serilog;

namespace FSH.Starter.WebApi.Host;

public static class Program
{
    public static async Task Main(string[] args)
    {
        StaticLogger.EnsureInitialized();
        Log.Information("server booting up..");
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // ConfigureFshFramework already sets up all services
            builder.ConfigureFshFramework();
            builder.RegisterModules();

            var app = builder.Build();

            // Run database migrations BEFORE configuring middleware
            await app.RunMigrationsAsync();
            
            app.UseFshFramework();
            app.UseModules();
            
            await app.RunAsync();
        }
        catch (Exception ex) when (!ex.GetType().Name.Equals("HostAbortedException", StringComparison.Ordinal))
        {
            StaticLogger.EnsureInitialized();
            Log.Fatal(ex.Message, "unhandled exception");
        }
        finally
        {
            StaticLogger.EnsureInitialized();
            Log.Information("server shutting down..");
            await Log.CloseAndFlushAsync();
        }
    }
}
