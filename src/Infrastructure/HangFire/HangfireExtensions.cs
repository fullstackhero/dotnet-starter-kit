using DN.WebApi.Application.Settings;
using DN.WebApi.Infrastructure.Multitenancy;
using Hangfire;
using Hangfire.Console;
using Hangfire.MySql;
using Hangfire.PostgreSql;
using Hangfire.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DN.WebApi.Infrastructure.HangFire;

public static class HangfireExtensions
{
    private static readonly ILogger _logger = Log.ForContext(typeof(HangfireExtensions));

    public static IServiceCollection AddHangFireService(this IServiceCollection services)
    {
        var storageSettings = services.GetOptions<HangFireStorageSettings>("HangFireSettings:Storage");

        if (string.IsNullOrEmpty(storageSettings.StorageProvider)) throw new Exception("Storage HangFire Provider is not configured.");
        _logger.Information($"Hangfire: Current Storage Provider : {storageSettings.StorageProvider}");
        _logger.Information("For more HangFire storage, visit https://www.hangfire.io/extensions.html");

        services.AddSingleton<JobActivator, ContextJobActivator>();

        switch (storageSettings.StorageProvider.ToLower())
        {
            case "postgresql":
                services.AddHangfire((provider, config) =>
                {
                    config.UsePostgreSqlStorage(storageSettings.ConnectionString, services.GetOptions<PostgreSqlStorageOptions>("HangFireSettings:Storage:Options"))
                    .UseFilter(new TenantJobFilter(provider))
                    .UseFilter(new LogJobFilter())
                    .UseConsole();
                });
                break;

            case "mssql":
                services.AddHangfire((provider, config) =>
                {
                    config.UseSqlServerStorage(storageSettings.ConnectionString, services.GetOptions<SqlServerStorageOptions>("HangFireSettings:Storage:Options"))
                    .UseFilter(new TenantJobFilter(provider))
                    .UseFilter(new LogJobFilter())
                    .UseConsole();
                });
                break;

            case "mysql":
                services.AddHangfire((provider, config) =>
                {
                    config.UseStorage(new MySqlStorage(storageSettings.ConnectionString, services.GetOptions<MySqlStorageOptions>("HangFireSettings:Storage:Options")))
                    .UseFilter(new TenantJobFilter(provider))
                    .UseFilter(new LogJobFilter())
                    .UseConsole();
                });
                break;

            default:
                throw new Exception($"HangFire Storage Provider {storageSettings.StorageProvider} is not supported.");
        }

        return services;
    }
}