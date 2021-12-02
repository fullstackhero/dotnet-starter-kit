using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Domain.Multitenancy;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
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

        services.AddDbContext<TenantManagementDbContext>(dbProvider, rootConnectionString);

        var scope = services.BuildServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantManagementDbContext>();

        if (dbContext.Database.GetPendingMigrations().Any())
        {
            _logger.Information("Applying Root Migrations.");
            dbContext.Database.Migrate();
        }

        SeedRootTenant(dbContext, rootConnectionString);

        foreach (var tenant in dbContext.Tenants.ToList())
        {
            services.SetupTenantDatabase(dbProvider, rootConnectionString, tenant);
        }

        _logger.Information("For documentations and guides, visit https://www.fullstackhero.net");
        _logger.Information("To Sponsor this project, visit https://opencollective.com/fullstackhero");
        return services;
    }

    private static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, string dbProvider, string rootConnectionString)
        where TContext : DbContext
    {
        switch (dbProvider.ToLower())
        {
            case "postgresql":
                services.AddDbContext<TContext>(m => m.UseNpgsql(rootConnectionString, e => e.MigrationsAssembly("Migrators.PostgreSQL")));
                break;

            case "mssql":
                services.AddDbContext<TContext>(m => m.UseSqlServer(rootConnectionString, e => e.MigrationsAssembly("Migrators.MSSQL")));
                break;

            case "mysql":
                services.AddDbContext<TContext>(m => m.UseMySql(rootConnectionString, ServerVersion.AutoDetect(rootConnectionString), e =>
                {
                    e.MigrationsAssembly("Migrators.MySQL");
                    e.SchemaBehavior(MySqlSchemaBehavior.Ignore);
                }));
                break;

            default:
                throw new Exception($"DB Provider {dbProvider} is not supported.");
        }

        return services;
    }

    private static void SeedRootTenant<T>(T dbContext, string connectionString)
    where T : TenantManagementDbContext
    {
        if (!dbContext.Tenants.Any(t => t.Key == MultitenancyConstants.Root.Key))
        {
            var rootTenant = new Tenant(
                MultitenancyConstants.Root.Name,
                MultitenancyConstants.Root.Key,
                MultitenancyConstants.Root.EmailAddress,
                connectionString);
            rootTenant.SetValidity(DateTime.UtcNow.AddYears(1));
            dbContext.Tenants.Add(rootTenant);
            dbContext.SaveChanges();
        }
    }

    private static IServiceCollection SetupTenantDatabase(this IServiceCollection services, string dbProvider, string rootConnectionString, Tenant tenant)
    {
        string tenantConnectionString = string.IsNullOrEmpty(tenant.ConnectionString) ? rootConnectionString : tenant.ConnectionString;

        services.AddDbContext<ApplicationDbContext>(dbProvider, tenantConnectionString);

        var scope = services.BuildServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var seeders = scope.ServiceProvider.GetServices<IDatabaseSeeder>().ToList();
        TenantBootstrapper.Initialize(dbContext, dbProvider, rootConnectionString, tenant, userManager, roleManager, seeders);
        return services;
    }
}