using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Persistence;

/// <summary>
/// Extension methods for configuring persistence services and database contexts.
/// </summary>
public static class PersistenceExtensions
{
    /// <summary>
    /// Adds database configuration options to the service collection with validation.
    /// </summary>
    /// <param name="services">The service collection to add options to.</param>
    /// <param name="configuration">The configuration instance containing database settings.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public static IServiceCollection AddHeroDatabaseOptions(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(nameof(DatabaseOptions)))
            .ValidateDataAnnotations()
            .Validate(o => !string.IsNullOrWhiteSpace(o.Provider), "DatabaseOptions.Provider is required.")
            .ValidateOnStart();
        services.AddHostedService<DatabaseOptionsStartupLogger>();
        return services;
    }

    /// <summary>
    /// Adds a configured Entity Framework DbContext to the service collection.
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext to configure.</typeparam>
    /// <param name="services">The service collection to add the context to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddHeroDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<TContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<IHostEnvironment>();
            var dbConfig = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.ConfigureHeroDatabase(dbConfig.Provider, dbConfig.ConnectionString, dbConfig.MigrationsAssembly, env.IsDevelopment());
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        });
        return services;
    }
}
