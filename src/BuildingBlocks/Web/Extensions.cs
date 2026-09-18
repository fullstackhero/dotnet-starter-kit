using FSH.Framework.Caching;
using FSH.Framework.Jobs;
using FSH.Framework.Mailing;
using FSH.Framework.Persistence;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Auth;
using FSH.Framework.Web.Cors;
using FSH.Framework.Web.Exceptions;
using FSH.Framework.Web.FeatureFlags;
using FSH.Framework.Web.Idempotency;
using FSH.Framework.Web.Localization;
using FSH.Framework.Web.Sse;
using FSH.Framework.Web.Health;
using FSH.Framework.Web.Mediator.Behaviors;
using FSH.Framework.Web.Modules;
using FSH.Framework.Web.Observability.Logging.Serilog;
using FSH.Framework.Web.Observability.OpenTelemetry;
using FSH.Framework.Web.OpenApi;
using FSH.Framework.Web.Origin;
using FSH.Framework.Web.RateLimiting;
using FSH.Framework.Web.Realtime;
using FSH.Framework.Web.Security;
using FSH.Framework.Web.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Mediator;

namespace FSH.Framework.Web;

public static class Extensions
{
    public static IHostApplicationBuilder AddHeroPlatform(this IHostApplicationBuilder builder, Action<FshPlatformOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new FshPlatformOptions();
        configure?.Invoke(options);

        PermissionConstants.Register(SystemPermissions.All);

        builder.Services.AddScoped<CurrentUserMiddleware>();

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        });

        builder.AddHeroLogging();
        if (options.EnableOpenTelemetry)
        {
            builder.AddHeroOpenTelemetry();
        }

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHeroDatabaseOptions(builder.Configuration);
        builder.Services.AddHeroRateLimiting(builder.Configuration);

        var corsEnabled = options.EnableCors && IsCorsEnabled(builder.Configuration);
        var openApiEnabled = options.EnableOpenApi && IsOpenApiEnabled(builder.Configuration);

        if (corsEnabled)
        {
            builder.Services.AddHeroCors(builder.Configuration);
        }

        builder.Services.AddHeroVersioning();

        if (openApiEnabled)
        {
            builder.Services.AddHeroOpenApi(builder.Configuration);
        }

        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());

        if (options.EnableJobs)
        {
            builder.Services.AddHeroJobs();
            builder.Services.AddHealthChecks().AddCheck<HangfireHealthCheck>("hangfire");
        }

        if (options.EnableMailing)
        {
            builder.Services.AddHeroMailing();
        }

        if (options.EnableCaching)
        {
            builder.Services.AddHeroCaching(builder.Configuration);
            var cacheConfig = builder.Configuration.GetSection(nameof(CachingOptions)).Get<CachingOptions>();
            if (cacheConfig is not null && !string.IsNullOrEmpty(cacheConfig.Redis))
            {
                builder.Services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis");
            }
        }

        if (options.EnableFeatureFlags)
        {
            builder.Services.AddHeroFeatureFlags(builder.Configuration);
        }

        if (options.EnableIdempotency)
        {
            builder.Services.AddHeroIdempotency(builder.Configuration);
        }

        if (options.EnableSse)
        {
            builder.Services.AddHeroSse();
        }

        if (options.EnableRealtime)
        {
            builder.Services.AddHeroRealtime(builder.Configuration);
        }

        if (options.EnableQuotas)
        {
            builder.Services.AddHeroQuotas(builder.Configuration);
        }

        builder.Services.AddHeroLocalization(builder.Configuration);
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        builder.Services.AddProblemDetails();
        builder.Services.AddOptions<OriginOptions>().BindConfiguration(nameof(OriginOptions));
        builder.Services.AddOptions<SecurityHeadersOptions>().BindConfiguration(nameof(SecurityHeadersOptions));

        return builder;
    }


    public static WebApplication UseHeroPlatform(this WebApplication app, Action<FshPipelineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = new FshPipelineOptions();
        configure?.Invoke(options);

        var corsEnabled = options.UseCors && IsCorsEnabled(app.Configuration);
        var openApiEnabled = options.UseOpenApi && IsOpenApiEnabled(app.Configuration);

        app.UseExceptionHandler();
        app.UseResponseCompression();

        // CORS MUST run before UseHttpsRedirection: preflight OPTIONS can't follow an HTTP→HTTPS redirect, so
        // the browser would block the call. Safe before routing because we use one global policy (no [EnableCors]).
        if (corsEnabled)
        {
            app.UseHeroCors();
        }

        app.UseHttpsRedirection();

        app.UseHeroSecurityHeaders();

        // Serve static files as early as possible to short-circuit pipeline
        if (options.ServeStaticFiles)
        {
            var assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (!Directory.Exists(assetsPath))
            {
                Directory.CreateDirectory(assetsPath);
            }

            app.UseStaticFiles();
        }

        app.UseHeroJobDashboard(app.Configuration);
        app.UseRouting();

        if (openApiEnabled)
        {
            app.UseHeroOpenApi();
        }

        app.UseAuthentication();

        // Must run after UseAuthentication so the user-locale-claim provider sees HttpContext.User,
        // and before UseAuthorization/endpoints so the request culture is set for the handlers.
        app.UseHeroLocalization();

        // Let each module register its own middleware (e.g. Auditing registers AuditHttpMiddleware)
        app.UseModuleMiddlewares();

        app.UseHeroRateLimiting();

        if (options.UseQuotas)
        {
            app.UseHeroQuotas();
        }

        app.UseAuthorization();

        if (options.MapModules)
        {
            app.MapModules();
        }

        // Always expose health endpoints
        app.MapHeroHealthEndpoints();

        if (options.MapSseEndpoints)
        {
            app.MapHeroSseEndpoints();
        }

        if (options.MapRealtime)
        {
            app.MapHeroRealtime();
        }
        app.UseMiddleware<CurrentUserMiddleware>();
        return app;
    }

    private static bool IsCorsEnabled(IConfiguration configuration)
    {
        var allowAll = configuration.GetValue("CorsOptions:AllowAll", false);
        var origins = configuration.GetSection("CorsOptions:AllowedOrigins").Get<string[]>() ?? [];
        return allowAll || origins.Length > 0;
    }

    private static bool IsOpenApiEnabled(IConfiguration configuration)
    {
        return configuration.GetValue("OpenApiOptions:Enabled", true);
    }
}

public sealed class FshPlatformOptions
{
    public bool EnableCors { get; set; } = true;
    public bool EnableOpenApi { get; set; } = true;
    public bool EnableCaching { get; set; } = false;
    public bool EnableJobs { get; set; } = false;
    public bool EnableMailing { get; set; } = false;
    public bool EnableOpenTelemetry { get; set; } = true;
    public bool EnableFeatureFlags { get; set; } = false;
    public bool EnableIdempotency { get; set; } = true;
    public bool EnableSse { get; set; } = false;
    public bool EnableRealtime { get; set; } = false;
    public bool EnableQuotas { get; set; } = false;
}

public sealed class FshPipelineOptions
{
    public bool UseCors { get; set; } = true;
    public bool UseOpenApi { get; set; } = true;
    public bool ServeStaticFiles { get; set; } = true;
    public bool MapModules { get; set; } = true;
    public bool MapSseEndpoints { get; set; } = false;
    public bool MapRealtime { get; set; } = false;
    public bool UseQuotas { get; set; } = false;
}