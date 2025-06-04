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
using FSH.Framework.Infrastructure.Jobs;
using FSH.Framework.Infrastructure.Logging.Serilog;
using FSH.Framework.Infrastructure.Mail;
using FSH.Framework.Infrastructure.OpenApi;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.RateLimit;
using FSH.Framework.Infrastructure.SecurityHeaders;
using FSH.Framework.Infrastructure.Storage.Files;
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

        // Register repositories & services
        builder.Services.AddScoped<FSH.Framework.Core.Auth.Repositories.IUserRepository, Auth.DapperUserRepository>();
        builder.Services.AddSingleton<FSH.Framework.Core.Auth.Services.IJwtTokenGenerator, Auth.Jwt.JwtTokenGenerator>();
        builder.Services.AddScoped<FSH.Framework.Core.Auth.Services.IValidationService, Auth.ValidationService>();
        builder.Services.AddScoped<FSH.Framework.Core.Auth.Features.Login.ITokenService, Auth.Jwt.TokenService>();
        builder.Services.AddScoped<FSH.Framework.Core.Auth.Services.ISmsService, Services.SmsService>();
        builder.Services.AddScoped<FSH.Framework.Core.Auth.Services.IVerificationService, Auth.VerificationService>();
        builder.Services.AddOptions<Auth.VerificationOptions>().BindConfiguration(nameof(Auth.VerificationOptions));
        
        // Register MERNİS Identity Verification Service with HttpClient
        builder.Services.AddHttpClient<FSH.Framework.Core.Auth.Services.IIdentityVerificationService, Auth.MernisIdentityVerificationService>(client =>
        {
            var timeoutSecondsString = builder.Configuration["MernisService:TimeoutSeconds"];
            var timeoutSeconds = int.TryParse(timeoutSecondsString, out var parsed) ? parsed : 30;
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        builder.Services.AddSingleton<FSH.Framework.Core.Auth.Repositories.IRefreshTokenRepository, Auth.InMemoryRefreshTokenRepository>();

        return builder;
    }

    public static WebApplication UseFshFramework(this WebApplication app)
    {
        app.MapDefaultEndpoints();
        app.UseRateLimit();
        app.UseSecurityHeaders();
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

        return app;
    }
}
