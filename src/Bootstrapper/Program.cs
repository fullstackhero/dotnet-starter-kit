using DN.WebApi.Application.DependencyInjection;
using DN.WebApi.Application.Extensions;
using DN.WebApi.Bootstrapper.Extensions;
using DN.WebApi.Infrastructure.Extensions;
using FluentValidation.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
Log.Information("Server Booting Up...");
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.AddConfigurations();
    builder.Host.UseSerilog((_, config) => config.WriteTo.Console().ReadFrom.Configuration(builder.Configuration));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers().AddFluentValidation();

    var app = builder.Build();

    app.UseInfrastructure(builder.Configuration);
    app.Run();
}
catch (Exception ex)
{
    // Don't catch StopTheHostException as otherwise EF Core Migrations don't work (amongst other things).
    // See https://github.com/dotnet/runtime/issues/60600 for why this is handled this way.
    if (ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
    {
        throw;
    }

    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Server Shutting down...");
    Log.CloseAndFlush();
}