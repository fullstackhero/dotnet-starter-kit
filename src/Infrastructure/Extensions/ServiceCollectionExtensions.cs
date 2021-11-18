using System.Collections.Generic;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Infrastructure.Localizer;
using DN.WebApi.Infrastructure.Mappings;
using DN.WebApi.Infrastructure.Persistence;
using DN.WebApi.Infrastructure.Persistence.Extensions;
using DN.WebApi.Infrastructure.Services.General;
using Hangfire;
using Hangfire.Console.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            MapsterSettings.Configure();
            if (config.GetSection("CacheSettings:PreferRedis").Get<bool>())
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = config.GetSection("CacheSettings:RedisURL").Get<string>();
                    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
                    {
                        AbortOnConnectFail = true
                    };
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }

            services.TryAdd(ServiceDescriptor.Singleton<ICacheService, CacheService>());
            services.AddHealthCheckExtension();
            services.AddLocalization();
            services.AddServices();
            services.AddSettings(config);
            services.AddPermissions();
            services.AddIdentity(config);
            services.AddHangfireServer(options =>
            {
                var optionsServer = services.GetOptions<BackgroundJobServerOptions>("HangFireSettings:Server");
                options.HeartbeatInterval = optionsServer.HeartbeatInterval;
                options.Queues = optionsServer.Queues;
                options.SchedulePollingInterval = optionsServer.SchedulePollingInterval;
                options.ServerCheckInterval = optionsServer.ServerCheckInterval;
                options.ServerName = optionsServer.ServerName;
                options.ServerTimeout = optionsServer.ServerTimeout;
                options.ShutdownTimeout = optionsServer.ShutdownTimeout;
                options.WorkerCount = optionsServer.WorkerCount;
            });
            services.AddHangfireConsoleExtensions();
            services.AddMultitenancy<TenantManagementDbContext, ApplicationDbContext>(config);
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddMiddlewares();
            services.AddSwaggerDocumentation();
            services.AddCorsPolicy();
            services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ReportApiVersions = true;
            });
            services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
            return services;
        }

        public static IServiceCollection AddPermissions(this IServiceCollection services)
        {
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>()
                .AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            return services;
        }
    }
}