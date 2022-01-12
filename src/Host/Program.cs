global using DN.WebApi.Application.Common.Interfaces;
global using DN.WebApi.Application.Common.Models;
global using DN.WebApi.Infrastructure.Auth.Permissions;
global using DN.WebApi.Infrastructure.OpenApi;
global using DN.WebApi.Shared.Authorization;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using NSwag.Annotations;
using DN.WebApi.Application;
using DN.WebApi.Host.Configurations;
using DN.WebApi.Host.Controllers;
using DN.WebApi.Infrastructure;
using DN.WebApi.Infrastructure.Persistence;
using FluentValidation.AspNetCore;
using Serilog;

[assembly: ApiConventionType(typeof(FSHApiConventions))]

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
Log.Information("Server Booting Up...");
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.AddConfigurations();
    builder.Host.UseSerilog((_, config) =>
    {
        config.WriteTo.Console()
        .ReadFrom.Configuration(builder.Configuration);
    });

    builder.Services.AddControllers().AddFluentValidation();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    DatabaseInitializer.InitializeDatabases(app.Services);

    app.UseInfrastructure(builder.Configuration);

    app.Run();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Server Shutting down...");
    Log.CloseAndFlush();
}