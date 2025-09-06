using System.Reflection;
using Asp.Versioning.Conventions;
using FluentValidation;
using FSH.Framework.Core;
using FSH.Framework.Core.Origin;
using FSH.Framework.Infrastructure.Auth;
using FSH.Framework.Infrastructure.Auth.Jwt;
using FSH.Framework.Infrastructure.Behaviours;
using FSH.Framework.Infrastructure.Caching;
using FSH.Framework.Infrastructure.Cors;
using FSH.Framework.Infrastructure.Exceptions;
using FSH.Framework.Infrastructure.Identity;
using FSH.Framework.Infrastructure.Jobs;
using FSH.Framework.Infrastructure.Logging.Serilog;
using FSH.Framework.Infrastructure.Mail;
using FSH.Framework.Infrastructure.OpenApi;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.RateLimit;
using FSH.Framework.Infrastructure.SecurityHeaders;
using FSH.Framework.Infrastructure.Storage.Files;
using FSH.Framework.Infrastructure.Tenant;
using FSH.Framework.Infrastructure.Tenant.Endpoints;
using FSH.Starter.Aspire.ServiceDefaults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace FSH.Framework.Infrastructure;

public static class Extensions
{
    public static WebApplicationBuilder ConfigureFshFramework(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddServiceDefaults();
        builder.ConfigureSerilog();
        builder.ConfigureDatabase();
        builder.Services.ConfigureMultitenancy();
        builder.Services.ConfigureIdentity();
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.ConfigureFileStorage();
        builder.Services.ConfigureJwtAuth();
        builder.Services.ConfigureOpenApi();
        builder.Services.ConfigureJobs(builder.Configuration);
        builder.Services.ConfigureMailing();
        builder.Services.ConfigureCaching(builder.Configuration);
        builder.Services.AddExceptionHandler<CustomExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.AddOptions<OriginOptions>().BindConfiguration(nameof(OriginOptions));

        // Define module assemblies
        var assemblies = new Assembly[]
        {
            typeof(FshCore).Assembly,
            typeof(FshInfrastructure).Assembly
        };

        // Register validators
        builder.Services.AddValidatorsFromAssemblies(assemblies);

        // Register MediatR
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        builder.Services.ConfigureRateLimit(builder.Configuration);
        builder.Services.ConfigureSecurityHeaders(builder.Configuration);

        return builder;
    }

    public static WebApplication UseFshFramework(this WebApplication app)
    {
        app.MapDefaultEndpoints();
        app.UseRateLimit();
        app.UseSecurityHeaders();
        app.UseMultitenancy();
        app.UseExceptionHandler();
        app.UseCorsPolicy();
        app.UseOpenApi();
        app.UseJobDashboard(app.Configuration);
        app.UseRouting();
        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "assets")),
            RequestPath = new PathString("/assets")
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapTenantEndpoints();
        app.MapIdentityEndpoints();

        // Current user middleware
        app.UseMiddleware<CurrentUserMiddleware>();

        // Register API versions
        var versions = app.NewApiVersionSet()
                    .HasApiVersion(1)
                    .HasApiVersion(2)
                    .ReportApiVersions()
                    .Build();

        // Map versioned endpoint
        app.MapGroup("api/v{version:apiVersion}").WithApiVersionSet(versions);

        return app;
    }
}
