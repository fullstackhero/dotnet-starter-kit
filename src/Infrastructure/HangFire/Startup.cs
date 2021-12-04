using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.MySql;
using Hangfire.PostgreSql;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DN.WebApi.Infrastructure.Hangfire;

internal static class Startup
{
    private static readonly ILogger _logger = Log.ForContext(typeof(Startup));

    internal static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration appConfig)
    {
        services.AddHangfireServer(options =>
        {
            var serverSettings = appConfig.GetSection("HangfireSettings:Server").Get<BackgroundJobServerOptions>();
            options.HeartbeatInterval = serverSettings.HeartbeatInterval;
            options.Queues = serverSettings.Queues;
            options.SchedulePollingInterval = serverSettings.SchedulePollingInterval;
            options.ServerCheckInterval = serverSettings.ServerCheckInterval;
            options.ServerName = serverSettings.ServerName;
            options.ServerTimeout = serverSettings.ServerTimeout;
            options.ShutdownTimeout = serverSettings.ShutdownTimeout;
            options.WorkerCount = serverSettings.WorkerCount;
        });

        services.AddHangfireConsoleExtensions();

        var storageSettings = appConfig.GetSection("HangfireSettings:Storage").Get<HangfireStorageSettings>();

        if (string.IsNullOrEmpty(storageSettings.StorageProvider)) throw new Exception("Hangfire Storage Provider is not configured.");
        _logger.Information($"Hangfire: Current Storage Provider : {storageSettings.StorageProvider}");
        _logger.Information("For more Hangfire storage, visit https://www.hangfire.io/extensions.html");

        services.AddSingleton<JobActivator, FSHJobActivator>();

        switch (storageSettings.StorageProvider.ToLower())
        {
            case "postgresql":
                services.AddHangfire((provider, config) =>
                {
                    config.UsePostgreSqlStorage(storageSettings.ConnectionString, appConfig.GetSection("HangfireSettings:Storage:Options").Get<PostgreSqlStorageOptions>())
                    .UseFilter(new TenantJobFilter(provider))
                    .UseFilter(new LogJobFilter())
                    .UseConsole();
                });
                break;

            case "mssql":
                services.AddHangfire((provider, config) =>
                {
                    config.UseSqlServerStorage(storageSettings.ConnectionString, appConfig.GetSection("HangfireSettings:Storage:Options").Get<SqlServerStorageOptions>())
                    .UseFilter(new TenantJobFilter(provider))
                    .UseFilter(new LogJobFilter())
                    .UseConsole();
                });
                break;

            case "mysql":
                services.AddHangfire((provider, config) =>
                {
                    config.UseStorage(new MySqlStorage(storageSettings.ConnectionString, appConfig.GetSection("HangfireSettings:Storage:Options").Get<MySqlStorageOptions>()))
                    .UseFilter(new TenantJobFilter(provider))
                    .UseFilter(new LogJobFilter())
                    .UseConsole();
                });
                break;

            default:
                throw new Exception($"Hangfire Storage Provider {storageSettings.StorageProvider} is not supported.");
        }

        return services;
    }

    internal static IApplicationBuilder UseHangfireDashboard(this IApplicationBuilder app, IConfiguration config)
    {
        var configDashboard = config.GetSection("HangfireSettings:Dashboard").Get<DashboardOptions>();
        return app.UseHangfireDashboard(config["HangfireSettings:Route"], new DashboardOptions
        {
            DashboardTitle = configDashboard.DashboardTitle,
            StatsPollingInterval = configDashboard.StatsPollingInterval,
            AppPath = configDashboard.AppPath

            // ** OPtional BasicAuthAuthorizationFilter **
            // Authorization = new[] { new BasicAuthAuthorizationFilter(
            //    new BasicAuthAuthorizationFilterOptions {
            //        RequireSsl = false,
            //        SslRedirect = false,
            //        LoginCaseSensitive = true,
            //        Users = new []
            //        {
            //            new BasicAuthAuthorizationUser
            //            {
            //                Login = config["HangfireSettings:Credentiales:User"],
            //                PasswordClear =  config["HangfireSettings:Credentiales:Password"]
            //            }
            //        }
            //    })
            // }
        });
    }
}