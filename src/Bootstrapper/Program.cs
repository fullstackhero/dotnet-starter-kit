using System;
using System.IO;
using System.Text;
using DN.WebApi.Application.Extensions;
using DN.WebApi.Infrastructure.Extensions;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
Log.Information("API Booting Up...");
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console().ReadFrom.Configuration(ctx.Configuration));
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
    Log.Information("API Shutting down...");
    Log.CloseAndFlush();
}