using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using FL_CRMS_ERP_WEBAPI.Application;
using FL_CRMS_ERP_WEBAPI.Host.Configurations;
using FL_CRMS_ERP_WEBAPI.Host.Controllers;
using FL_CRMS_ERP_WEBAPI.Infrastructure;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Common;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Logging.Serilog;
using Serilog;
using Serilog.Formatting.Compact;
using sib_api_v3_sdk.Api;

[assembly: ApiConventionType(typeof(FLApiConventions))]

StaticLogger.EnsureInitialized();
Log.Information("Server Booting Up...");
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddConfigurations().RegisterSerilog();
    builder.Services.AddControllers();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddTransient<TransactionalSMSApi>();// Send Sms
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