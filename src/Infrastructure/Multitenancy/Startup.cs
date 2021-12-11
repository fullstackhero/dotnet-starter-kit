using DN.WebApi.Infrastructure.Persistence.Contexts;
using DN.WebApi.Shared.DTOs.Multitenancy;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Serilog;

namespace DN.WebApi.Infrastructure.Multitenancy;

internal static class Startup
{
    private static readonly ILogger _logger = Log.ForContext(typeof(Startup));

    internal static IServiceCollection AddCurrentTenant(this IServiceCollection services) =>
        services.AddScoped<CurrentTenantMiddleware>();

    internal static IApplicationBuilder UseCurrentTenant(this IApplicationBuilder app) =>
        app.UseMiddleware<CurrentTenantMiddleware>();

    internal static IServiceCollection AddMultitenancy(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<DatabaseSettings>(config.GetSection(nameof(DatabaseSettings)));
        var databaseSettings = config.GetSection(nameof(DatabaseSettings)).Get<DatabaseSettings>();
        string? rootConnectionString = databaseSettings.ConnectionString;
        if (string.IsNullOrEmpty(rootConnectionString)) throw new InvalidOperationException("DB ConnectionString is not configured.");
        string? dbProvider = databaseSettings.DBProvider;
        if (string.IsNullOrEmpty(dbProvider)) throw new InvalidOperationException("DB Provider is not configured.");
        _logger.Information($"Current DB Provider : {dbProvider}");

        return services
            .AddDbContext<TenantManagementDbContext>(m => m.UseDatabase(dbProvider, rootConnectionString))
            .AddDbContext<ApplicationDbContext>(m => m.UseDatabase(dbProvider, rootConnectionString));
    }

    private static DbContextOptionsBuilder UseDatabase(this DbContextOptionsBuilder builder, string dbProvider, string connectionString) =>
        dbProvider.ToLower() switch
        {
            "postgresql" =>
                builder.UseNpgsql(connectionString, e => e.MigrationsAssembly("Migrators.PostgreSQL")),
            "mssql" =>
                builder.UseSqlServer(connectionString, e => e.MigrationsAssembly("Migrators.MSSQL")),
            "mysql" =>
                builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), e =>
                    {
                        e.MigrationsAssembly("Migrators.MySQL");
                        e.SchemaBehavior(MySqlSchemaBehavior.Ignore);
                    }),
            "oracle" =>
                builder.UseOracle(connectionString, e => e.MigrationsAssembly("Migrators.Oracle")),
            _ => throw new Exception($"DB Provider {dbProvider} is not supported.")
        };
}