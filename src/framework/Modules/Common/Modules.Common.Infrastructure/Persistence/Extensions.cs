using FSH.Framework.Core.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace FSH.Framework.Infrastructure.Persistence;
public static class Extensions
{
    private static readonly ILogger Logger = Log.ForContext(typeof(Extensions));
    public static DbContextOptionsBuilder ConfigureDatabase(this DbContextOptionsBuilder builder,
        string dbProvider,
        string connectionString,
        string migrationsAssembly
        )
    {
        builder.ConfigureWarnings(warnings => warnings.Log(RelationalEventId.PendingModelChangesWarning));
        return dbProvider.ToUpperInvariant() switch
        {
            DbProviders.PostgreSQL =>
            builder.UseNpgsql(
                connectionString, e =>
                {
                    e.MigrationsAssembly(migrationsAssembly);
                })
                .EnableSensitiveDataLogging(),
            DbProviders.MSSQL =>
            builder.UseSqlServer(
                connectionString, e =>
                {
                    e.MigrationsAssembly(migrationsAssembly);
                }),
            _ => throw new InvalidOperationException($"DB Provider {dbProvider} is not supported."),
        };
    }

    public static WebApplicationBuilder AddDatabaseOption(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOptions<DatabaseOptions>()
            .BindConfiguration(nameof(DatabaseOptions))
            .ValidateDataAnnotations()
            .PostConfigure(config =>
            {
                Logger.Information("current db provider: {DatabaseProvider}", config.Provider);
                Logger.Information("for documentations and guides, visit https://www.fullstackhero.net");
                Logger.Information("to sponsor this project, visit https://opencollective.com/fullstackhero");
            });
        return builder;
    }

    public static IServiceCollection BindDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<TContext>((sp, options) =>
        {
            var dbConfig = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.ConfigureDatabase(dbConfig.Provider, dbConfig.ConnectionString, dbConfig.MigrationsAssembly);
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        });
        return services;
    }
}