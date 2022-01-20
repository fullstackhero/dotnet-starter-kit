global using FSH.WebApi.Application.Common.Interfaces;
global using FSH.WebApi.Application.Common.Models;
global using FSH.WebApi.Infrastructure.Auth.Permissions;
global using FSH.WebApi.Infrastructure.OpenApi;
global using FSH.WebApi.Shared.Authorization;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using NSwag.Annotations;
using FluentValidation.AspNetCore;
using FSH.WebApi.Application;
using FSH.WebApi.Host.Configurations;
using FSH.WebApi.Host.Controllers;
using FSH.WebApi.Infrastructure;
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

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers().AddFluentValidation();

    var app = builder.Build();

    await app.Services.InitializeDatabasesAsync();

    app.UseInfrastructure(builder.Configuration);
    app.MapEndpoints();

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