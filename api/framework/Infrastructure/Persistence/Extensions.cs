using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace FSH.Framework.Infrastructure.Persistence;
public static class Extensions
{
    private static readonly ILogger _logger = Log.ForContext(typeof(Extensions));
    internal static DbContextOptionsBuilder ConfigureDatabase(this DbContextOptionsBuilder builder, string dbProvider, string connectionString)
    {
        return dbProvider.ToUpperInvariant() switch
        {
            DbProviders.PostgreSQL => builder.UseNpgsql(connectionString, e =>
                                 e.MigrationsAssembly("FSH.WebApi.Migrations.PostgreSQL")),
            _ => throw new InvalidOperationException($"DB Provider {dbProvider} is not supported."),
        };
    }

    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOptions<DbConfig>()
            .BindConfiguration(nameof(DbConfig))
            .PostConfigure(config =>
            {
                if (config.UseInMemoryDb)
                {
                    _logger.Information("using in-memory database..");
                }
                else
                {
                    _logger.Information("current db provider: {dbProvider}", config.Provider);
                }

            });
        return builder;
    }

    public static IServiceCollection BindDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<TContext>((p, options) =>
        {
            var dbConfig = p.GetRequiredService<IOptions<DbConfig>>().Value;
            if (dbConfig.UseInMemoryDb)
            {
                options.UseInMemoryDatabase("fshdb");
            }
            else
            {
                options.ConfigureDatabase(dbConfig.Provider, dbConfig.ConnectionString);
            }
        });
        return services;
    }
}
