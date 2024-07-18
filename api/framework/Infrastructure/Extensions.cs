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
using FSH.Framework.Infrastructure.HealthChecks;
using FSH.Framework.Infrastructure.Identity;
using FSH.Framework.Infrastructure.Jobs;
using FSH.Framework.Infrastructure.Logging.Serilog;
using FSH.Framework.Infrastructure.Mail;
using FSH.Framework.Infrastructure.Observability;
using FSH.Framework.Infrastructure.OpenApi;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.RateLimit;
using FSH.Framework.Infrastructure.SecurityHeaders;
using FSH.Framework.Infrastructure.Storage.Files;
using FSH.Framework.Infrastructure.Tenant;
using FSH.Framework.Infrastructure.Tenant.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure;

public static class Extensions
{
    public static WebApplicationBuilder RegisterFshFramework(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureSerilog();
        builder.ConfigureDatabase();
        builder.ConfigureObservability();
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
            typeof(FshCore).Assembly
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
        app.UseRateLimit();
        app.UseSecurityHeaders();
        app.UseHttpsRedirection();
        app.UseHsts();
        app.UseMultitenancy();
        app.UseExceptionHandler();
        app.UseCorsPolicy();
        app.UseOpenApi();
        app.UseJobDashboard(app.Configuration);
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapTenantEndpoints();
        app.MapIdentityEndpoints();

        var health = app.MapGroup("api/health").WithTags("health");
        health.MapCustomHealthCheckEndpoint();

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
