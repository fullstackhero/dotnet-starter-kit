using FSH.Framework.Persistence;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Auditing;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers auditing core: Channel publisher, background worker, serializer, scope, and HTTP options.
    /// </summary>
    public static IServiceCollection AddAuditingCore(this IServiceCollection services, IConfiguration config, Action<AuditHttpOptions>? configure = null)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IAuditClient, DefaultAuditClient>();
        services.AddScoped<ISecurityAudit, SecurityAudit>();
        services.AddHeroDbContext<AuditDbContext>();
        services.AddSingleton<IAuditSerializer, SystemTextJsonAuditSerializer>();

        // Request-scoped scope reader (HttpContext-backed)
        services.AddScoped<IAuditScope, HttpAuditScope>();

        // Publisher/sink/worker wiring: publisher is singleton and resolves current scope from HttpContext.
        services.AddSingleton<ChannelAuditPublisher>();
        services.AddSingleton<IAuditPublisher>(sp => sp.GetRequiredService<ChannelAuditPublisher>());

        services.AddHostedService<AuditBackgroundWorker>();
        services.AddSingleton<IAuditSink, SqlAuditSink>();

        var opts = new AuditHttpOptions();
        configure?.Invoke(opts);
        services.AddSingleton(opts);

        return services;
    }

    /// <summary>
    /// Adds the HTTP auditing middleware to the pipeline.
    /// Place early (after routing) but before endpoints.
    /// </summary>
    public static IApplicationBuilder UseAuditHttp(this IApplicationBuilder app)
        => app.UseMiddleware<AuditHttpMiddleware>();
}
