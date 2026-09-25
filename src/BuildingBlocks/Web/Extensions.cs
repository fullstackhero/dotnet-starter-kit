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
using FSH.Framework.Web.Frontend;
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
using FSH.Framework.Web.TrustedProxy;
using FSH.Framework.Web.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mediator;
using System.Net;

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

        // The app runs behind a reverse proxy (e.g. cloudflared → Caddy → app), so the real client IP
        // and scheme arrive via X-Forwarded-*. Without this, RemoteIpAddress is the proxy's container
        // IP, which collapses the rate-limit partition into one bucket and records useless audit IPs.
        // Trust is bound to the configured ingress CIDRs/proxies (see TrustedProxyOptions): forwarded
        // headers from any other source are ignored, so a client reaching the app directly cannot forge
        // its IP/scheme. With nothing configured, the framework default (loopback only) stands.
        var trustedProxy = builder.Configuration
            .GetSection(nameof(TrustedProxyOptions)).Get<TrustedProxyOptions>() ?? new TrustedProxyOptions();
        builder.Services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            // A hop count below 1 is never what an operator means, and neither bad value announces itself:
            // 0 truncates the unwind loop to zero iterations, so forwarded headers stop being processed with
            // no error, while a negative value overflows the middleware's buffer allocation and 500s every
            // request - including requests carrying no forwarded headers at all. Fail the boot instead.
            if (trustedProxy.ForwardLimit < 1)
            {
                throw new InvalidOperationException(
                    $"{nameof(TrustedProxyOptions)}:{nameof(TrustedProxyOptions.ForwardLimit)} is {trustedProxy.ForwardLimit}, which is not a valid proxy hop count: it must be at least 1 (one hop per proxy in front of the app).");
            }

            forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            forwarded.ForwardLimit = trustedProxy.ForwardLimit;

            // The trust list is always rebuilt from scratch, never appended to. Whatever is in the
            // options when this runs depends on who configured them first, and with
            // ASPNETCORE_FORWARDEDHEADERS_ENABLED=true that is ForwardedHeadersOptionsSetup, which
            // empties both lists. An empty list is not "trust nobody" in ForwardedHeadersMiddleware:
            // it only validates the peer when at least one entry exists, so empty means the app
            // rewrites RemoteIpAddress from an X-Forwarded-For sent by anyone at all.
            forwarded.KnownProxies.Clear();
            forwarded.KnownIPNetworks.Clear();

            if (trustedProxy.KnownProxies.Length == 0 && trustedProxy.KnownNetworks.Length == 0)
            {
                // Nothing configured: restate the framework's own default rather than inherit it,
                // for the same reason. Local development runs behind Kestrel on loopback and still
                // needs its forwarded headers honoured.
                forwarded.KnownProxies.Add(IPAddress.IPv6Loopback);
                forwarded.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Loopback, 8));
                return;
            }

            foreach (var proxy in trustedProxy.KnownProxies)
            {
                if (!IPAddress.TryParse(proxy, out var address))
                {
                    throw new InvalidOperationException(
                        $"{nameof(TrustedProxyOptions)}:{nameof(TrustedProxyOptions.KnownProxies)} contains \"{proxy}\", which is not a valid IP address (for example \"10.0.0.5\").");
                }

                forwarded.KnownProxies.Add(address);
            }

            foreach (var network in trustedProxy.KnownNetworks)
            {
                if (!System.Net.IPNetwork.TryParse(network, out var parsedNetwork))
                {
                    throw new InvalidOperationException(
                        $"{nameof(TrustedProxyOptions)}:{nameof(TrustedProxyOptions.KnownNetworks)} contains \"{network}\", which is not a valid CIDR network (for example \"10.0.0.0/8\").");
                }

                forwarded.KnownIPNetworks.Add(parsedNetwork);
            }
        });

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

        // Front-end origin resolution for user-facing links in e-mails/notifications. There is no
        // fallback tier: the resolver throws when DefaultOrigin is unset. The API host fails fast on
        // it in Production (Program.cs); it is not validated here because the DbMigrator also calls
        // AddHeroPlatform and never sends links. Other environments get one startup Error instead.
        builder.Services.AddOptions<FrontendOptions>().BindConfiguration(nameof(FrontendOptions));
        builder.Services.AddScoped<IFrontendOriginResolver, FrontendOriginResolver>();

        return builder;
    }


    public static WebApplication UseHeroPlatform(this WebApplication app, Action<FshPipelineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        WarnOnMissingFrontendOrigin(app);

        var options = new FshPipelineOptions();
        configure?.Invoke(options);

        var corsEnabled = options.UseCors && IsCorsEnabled(app.Configuration);
        var openApiEnabled = options.UseOpenApi && IsOpenApiEnabled(app.Configuration);

        app.UseExceptionHandler();

        // Apply forwarded headers before anything reads the client IP or scheme (HTTPS redirect,
        // rate limiting, auth, audit) so they all see the real client, not the reverse proxy.
        app.UseForwardedHeaders();

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

    // One Warning at boot, never per request: the resolver is scoped, so logging there would either
    // flood the aggregator or stay silent on a host that simply never sends a link. An operator who
    // upgrades into this change reads it once, in the startup banner, with the fix in the message.
    private static void WarnOnMissingFrontendOrigin(WebApplication app)
    {
        var frontend = app.Services.GetRequiredService<IOptions<FrontendOptions>>().Value;

        // Reported independently of DefaultOrigin: a deployment that sets only the default still
        // has every self-service link falling back to it, which is wrong the moment there is more
        // than one front-end. Counted after normalization, so a list of nothing but unparseable
        // entries reports as the empty list it effectively is rather than looking configured.
        var usableOrigins = FrontendOriginResolver.Normalize(frontend.AllowedOrigins).Length;
        if (usableOrigins == 0)
        {
            app.Logger.LogWarning(
                "FrontendOptions:AllowedOrigins is empty or entirely unparseable (appsettings.{Environment}.json). Password-reset and self-registration links cannot follow the front-end that made the request and will all point at FrontendOptions:DefaultOrigin instead. With more than one front-end that sends users to the wrong app. List every SPA origin as an absolute URL, e.g. [ \"https://app.example.com\", \"https://admin.example.com\" ].",
                app.Environment.EnvironmentName);
        }
        else if (usableOrigins < frontend.AllowedOrigins.Length)
        {
            app.Logger.LogWarning(
                "{DroppedCount} of {ConfiguredCount} FrontendOptions:AllowedOrigins entries are not absolute URLs and were ignored (appsettings.{Environment}.json). Requests from those origins will be rejected with 400. Each entry must carry a scheme, e.g. \"https://app.example.com\".",
                frontend.AllowedOrigins.Length - usableOrigins,
                frontend.AllowedOrigins.Length,
                app.Environment.EnvironmentName);
        }

        if (Uri.TryCreate(frontend.DefaultOrigin, UriKind.Absolute, out _))
        {
            return;
        }

        // Absolute, not merely non-empty: "app.example.com" (no scheme, a common .env slip) binds
        // fine and then every link in every e-mail is a relative URL no mail client makes clickable.
        // Same failure class as an unset value, so it gets the same Error, not Warning: without a
        // usable default there is nothing left to build these links out of.
        // The resolver used to fall back to the API origin and then to the request host; both are
        // gone, because the links now address SPA paths (the API origin 404s them) and the request
        // host is caller-controlled (it hands the reset token to whoever set the Host header).
        app.Logger.LogError(
            "FrontendOptions:DefaultOrigin is not set to an absolute URL (appsettings.{Environment}.json). Admin register, resend confirmation, self-registration and password reset will return 500 for any caller that does not match FrontendOptions:AllowedOrigins, including every background job. Set FrontendOptions:DefaultOrigin to your dashboard URL, e.g. \"https://app.example.com\".",
            app.Environment.EnvironmentName);
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