using FSH.WebApi.Application;
using FSH.WebApi.Host.Configurations;
using FSH.WebApi.Host.Controllers;
using FSH.WebApi.Infrastructure;
using FSH.WebApi.Infrastructure.Common;
using FSH.WebApi.Infrastructure.Logging.Serilog;
using Serilog;
using Serilog.Formatting.Compact;
using System.Net;

[assembly: ApiConventionType(typeof(FSHApiConventions))]
StaticLogger.EnsureInitialized();
Log.Information("Server Booting Up...");

ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddConfigurations().RegisterSerilog();
    builder.Services.AddControllers();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();

    var app = builder.Build();

    await app.Services.InitializeDatabasesAsync();

    app.UseInfrastructure(builder.Configuration);
    app.MapEndpoints();
    app.Run();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("HostAbortedException", StringComparison.Ordinal))
{
    StaticLogger.EnsureInitialized();
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    StaticLogger.EnsureInitialized();
    Log.Information("Server Shutting down...");
    Log.CloseAndFlush();
}