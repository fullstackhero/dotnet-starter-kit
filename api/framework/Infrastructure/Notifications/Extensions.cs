using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FSH.Framework.Infrastructure.Notifications;

internal static class Extensions
{
    internal static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        ILogger logger = Log.ForContext(typeof(Extensions));
        
        SignalRSettings? signalRSettings = configuration.GetSection(nameof(SignalRSettings)).Get<SignalRSettings>();

        if (signalRSettings?.UseBackplane == true)
        {
            services.AddSignalR();
        }
        else
        {
            var backplaneSettings = configuration.GetSection("SignalRSettings:Backplane").Get<SignalRSettings.Backplane>();
            if (backplaneSettings is null) throw new InvalidOperationException("Backplane enabled, but no backplane settings in config.");
            switch (backplaneSettings.Provider)
            {
                case "redis":
                    if (backplaneSettings.StringConnection is null) throw new InvalidOperationException("Redis backplane provider: No connectionString configured.");
                    services.AddSignalR().AddStackExchangeRedis(backplaneSettings.StringConnection, options =>
                    {
                        options.Configuration.AbortOnConnectFail = false;
                    });
                    break;

                default:
                    throw new InvalidOperationException($"SignalR backplane Provider {backplaneSettings.Provider} is not supported.");
            }

            logger.Information("SignalR Backplane Current Provider: {Provider}.", backplaneSettings.Provider);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapNotifications(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationHub>("/notifications", options =>
        {
            options.CloseOnAuthenticationExpiration = true;
        });

        return endpoints;
    }
}
