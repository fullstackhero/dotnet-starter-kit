using DN.WebApi.Application.Settings;
using DN.WebApi.Infrastructure.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DN.WebApi.Infrastructure.Common.Extensions;

public static class SignalRExtensions
{
    internal static IServiceCollection AddNotifications(this IServiceCollection services)
    {
        ILogger logger = Log.ForContext(typeof(SignalRExtensions));

        var signalRSettings = services.GetOptions<SignalRSettings>("SignalRSettings");

        if (!signalRSettings.UseBackplane)
        {
            services.AddSignalR();
        }
        else
        {
            var backplaneSettings = services.GetOptions<SignalRSettings.Backplane>("SignalRSettings:Backplane");
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

            logger.Information($"SignalR Backplane Current Provider: {backplaneSettings.Provider}.");
        }

        return services;
    }
}