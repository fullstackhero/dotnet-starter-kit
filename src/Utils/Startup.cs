using FSH.WebApi.Utils.SourceGenerator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.WebApi.Utils;
public static class Startup
{
    public static IServiceCollection AddUtils(this IServiceCollection services, IConfiguration config)
    {
        return services
            .AddSourceGenerator(config);
    }

    internal static ConfigureHostBuilder AddConfigurations(this ConfigureHostBuilder utils)
    {
        utils.ConfigureAppConfiguration((context, config) =>
        {
            const string configurationsDirectory = "SourceGenerator";
            var env = context.HostingEnvironment;
            config.AddJsonFile($"{configurationsDirectory}/generatesources.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"{configurationsDirectory}/generatesources.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
        });
        return utils;
    }

    public static async Task InitializeGenerateSources(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IGenerateSources>()
            .InitializeAsync(cancellationToken);
    }
}


