using FluentValidation;
using FSH.Framework.Core;
using FSH.Framework.Infrastructure;
using FSH.Framework.Infrastructure.Caching;
using FSH.Framework.Infrastructure.Cors;
using FSH.Framework.Infrastructure.Exceptions;
using FSH.Framework.Infrastructure.Jobs;
using FSH.Framework.Infrastructure.Logging.Serilog;
using FSH.Framework.Infrastructure.Mail;
using FSH.Framework.Infrastructure.Messaging.CQRS;
using FSH.Framework.Infrastructure.Messaging.Events;
using FSH.Framework.Infrastructure.OpenApi;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.RateLimit;
using FSH.Framework.Infrastructure.SecurityHeaders;
using FSH.Framework.Infrastructure.Storage;
using FSH.Modules.Common.Core.Origin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace FSH.Modules.Common.Infrastructure;

public static class Extensions
{
    public static WebApplicationBuilder AddFshFramework(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddHttpContextAccessor();
        builder.AddFshSerilog();
        builder.AddDatabaseOption();
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.AddLocalFileStorage();
        builder.Services.AddFshOpenApi();
        builder.Services.AddFshJobs();
        builder.Services.AddFshMailing();
        builder.Services.AddFshCaching(builder.Configuration);
        builder.Services.AddExceptionHandler<CustomExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.AddOptions<OriginOptions>().BindConfiguration(nameof(OriginOptions));

        // Define framework assemblies
        var assemblies = new Assembly[]
        {
            typeof(FshCore).Assembly,
            typeof(FshInfrastructure).Assembly
        };

        // Register validators
        builder.Services.AddValidatorsFromAssemblies(assemblies);

        // register messaging services
        builder.Services.AddCommandAndQueryDispatchers();
        builder.Services.AddInMemoryEventBus(assemblies);

        builder.Services.AddRateLimiting(builder.Configuration);
        builder.Services.AddSecurityHeaders(builder.Configuration);

        return builder;
    }

    public static WebApplication ConfigureFshFramework(this WebApplication app)
    {
        app.UseRateLimit();
        app.UseSecurityHeaders();
        app.UseExceptionHandler();
        app.UseCorsPolicy();
        app.UseOpenApi();
        app.UseJobDashboard(app.Configuration);
        app.UseRouting();
        app.UseStaticFiles();

        app.MapHealthChecks("/health").AllowAnonymous();

        var assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        if (!Directory.Exists(assetsPath))
        {
            Directory.CreateDirectory(assetsPath);
        }
        app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
            RequestPath = new PathString("/wwwroot"),
        });

        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}