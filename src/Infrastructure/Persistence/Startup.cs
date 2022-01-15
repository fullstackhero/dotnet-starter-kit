using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Domain.Catalog.Brands;
using FSH.WebApi.Domain.Catalog.Products;
using FSH.WebApi.Infrastructure.Common;
using FSH.WebApi.Infrastructure.Persistence.ConnectionString;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Infrastructure.Persistence.Initialization;
using FSH.WebApi.Infrastructure.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Serilog;

namespace FSH.WebApi.Infrastructure.Persistence;

internal static class Startup
{
    private static readonly ILogger _logger = Log.ForContext(typeof(Startup));

    internal static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        // TODO: there must be a cleaner way to do IOptions validation...
        var databaseSettings = config.GetSection(nameof(DatabaseSettings)).Get<DatabaseSettings>();
        string? rootConnectionString = databaseSettings.ConnectionString;
        if (string.IsNullOrEmpty(rootConnectionString)) throw new InvalidOperationException("DB ConnectionString is not configured.");
        string? dbProvider = databaseSettings.DBProvider;
        if (string.IsNullOrEmpty(dbProvider)) throw new InvalidOperationException("DB Provider is not configured.");
        _logger.Information($"Current DB Provider : {dbProvider}");

        return services
            .Configure<DatabaseSettings>(config.GetSection(nameof(DatabaseSettings)))

            .AddDbContext<ApplicationDbContext>(m => m.UseDatabase(dbProvider, rootConnectionString))

            .AddTransient<IDatabaseInitializer, DatabaseInitializer>()
            .AddTransient<ApplicationDbInitializer>()
            .AddTransient<ApplicationDbSeeder>()
            .AddServices(typeof(ICustomSeeder), ServiceLifetime.Transient)
            .AddTransient<CustomSeederRunner>()

            .AddTransient<IConnectionStringSecurer, ConnectionStringSecurer>()
            .AddTransient<IConnectionStringValidator, ConnectionStringValidator>()

            // Add ApplicationDb Repositories
            .AddScoped(typeof(IRepository<>), typeof(ApplicationDbRepository<>))

            // Add ReadRepositories
            // TODO: figure out how to do this automatically for all IAgregateRoots
            .AddScoped(sp => (IReadRepository<Brand>)sp.GetRequiredService<IRepository<Brand>>())
            .AddScoped(sp => (IReadRepository<Product>)sp.GetRequiredService<IRepository<Product>>());
    }

    internal static DbContextOptionsBuilder UseDatabase(this DbContextOptionsBuilder builder, string dbProvider, string connectionString) =>
        dbProvider.ToLowerInvariant() switch
        {
            DbProviderKeys.Npgsql =>
                builder.UseNpgsql(connectionString, e =>
                    e.MigrationsAssembly("Migrators.PostgreSQL")),

            DbProviderKeys.SqlServer =>
                builder.UseSqlServer(connectionString, e =>
                    e.MigrationsAssembly("Migrators.MSSQL")),

            DbProviderKeys.MySql =>
                builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), e =>
                    e.MigrationsAssembly("Migrators.MySQL")
                     .SchemaBehavior(MySqlSchemaBehavior.Ignore)),

            DbProviderKeys.Oracle =>
                builder.UseOracle(connectionString, e =>
                    e.MigrationsAssembly("Migrators.Oracle")),

            _ => throw new InvalidOperationException($"DB Provider {dbProvider} is not supported.")
        };
}