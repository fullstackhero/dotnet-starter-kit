using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Settings;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence;
using DN.WebApi.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DN.WebApi.Infrastructure.Extensions
{
    public static class SignalrExtensions
    {
        internal static IServiceCollection AddNotifications(this IServiceCollection services)
        {
            ILogger logger = Log.ForContext(typeof(SignalrExtensions));

            var signalSettings = services.GetOptions<SignalSettings>("SignalRSettings");

            if (!signalSettings.UseBackplane)
            {
                services.AddSignalR();
            }
            else
            {
                var backplaneSettings = services.GetOptions<SignalSettings.Backplane>("SignalRSettings:Backplane");
                switch (backplaneSettings.Provider)
                {
                    case "redis":
                        services.AddSignalR().AddStackExchangeRedis(backplaneSettings.StringConnection, options =>
                        {
                            options.Configuration.AbortOnConnectFail = false;
                        });
                        break;

                    default:
                        throw new Exception($"SignalR backplane Provider {backplaneSettings.Provider} is not supported.");
                }

                logger.Information($"SignalR Backplane Current Provider: {backplaneSettings.Provider}.");
            }

            return services;
        }
    }
}