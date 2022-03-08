using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Infrastructure.Localization;
using FSH.WebApi.Infrastructure.Persistence.ConnectionString;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Test;

public class Startup
{
    public void ConfigureHost(IHostBuilder host) =>
        host.ConfigureHostConfiguration(config => config.AddJsonFile("appsettings.json"));

    public void ConfigureServices(IServiceCollection services, HostBuilderContext context) =>
        services
            .AddMemoryCache()
            .AddPOLocalization(context.Configuration)

            .AddTransient<IConnectionStringSecurer, ConnectionStringSecurer>();
}