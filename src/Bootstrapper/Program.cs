using System;
using DN.WebApi.Application.Extensions;
using DN.WebApi.Bootstrapper.Extensions;
using DN.WebApi.Infrastructure.Extensions;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

var configuration = new ConfigurationBuilder().AddJsonFile("Configurations/logger.json").Build();
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
Log.Information("Server Booting Up...");
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.AddConfigurations();
    builder.Host.UseSerilog((_, lc) => lc.WriteTo.Console().ReadFrom.Configuration(builder.Configuration));

    builder.Services.AddControllers().AddFluentValidation();
    builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
    var app = builder.Build();
    app.UseInfrastructure(builder.Configuration);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Server Shutting down...");
    Log.CloseAndFlush();
}