using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Settings;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Domain.Multitenancy;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Serilog;

namespace DN.WebApi.Infrastructure.Multitenancy;

public static class MultitenancyExtensions
{
    private static readonly ILogger _logger = Log.ForContext(typeof(MultitenancyExtensions));

    public static IServiceCollection AddMultitenancy<T, TA>(this IServiceCollection services, IConfiguration config)
    where T : TenantManagementDbContext
    where TA : ApplicationDbContext
    {
        services.Configure<DatabaseSettings>(config.GetSection(nameof(DatabaseSettings)));
        var databaseSettings = services.GetOptions<DatabaseSettings>(nameof(DatabaseSettings));
        string rootConnectionString = databaseSettings.ConnectionString;
        string dbProvider = databaseSettings.DBProvider;
        if (string.IsNullOrEmpty(dbProvider)) throw new Exception("DB Provider is not configured.");
        _logger.Information($"Current DB Provider : {dbProvider}");
        switch (dbProvider.ToLower())
        {
            case "postgresql":
                services.AddDbContext<T>(m => m.UseNpgsql(rootConnectionString, e => e.MigrationsAssembly("Migrators.PostgreSQL")));
                break;

            case "mssql":
                services.AddDbContext<T>(m => m.UseSqlServer(rootConnectionString, e => e.MigrationsAssembly("Migrators.MSSQL")));
                break;

            case "mysql":
                services.AddDbContext<T>(m => m.UseMySql(rootConnectionString, ServerVersion.AutoDetect(rootConnectionString), e =>
                {
                    e.MigrationsAssembly("Migrators.MySQL");
                    e.SchemaBehavior(MySqlSchemaBehavior.Ignore);
                }));
                break;

            default:
                throw new Exception($"DB Provider {dbProvider} is not supported.");
        }

        services.SetupDatabases<T, TA>(databaseSettings);
        _logger.Information("For documentations and guides, visit https://www.fullstackhero.net");
        _logger.Information("To Sponsor this project, visit https://opencollective.com/fullstackhero");
        return services;
    }

    private static IServiceCollection SetupDatabases<T, TA>(this IServiceCollection services, DatabaseSettings options)
    where T : TenantManagementDbContext
    where TA : ApplicationDbContext
    {
        var scope = services.BuildServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<T>();
        dbContext.Database.SetConnectionString(options.ConnectionString);
        switch (options.DBProvider.ToLower())
        {
            case "postgresql":
                services.AddDbContext<TA>(m => m.UseNpgsql(e => e.MigrationsAssembly("Migrators.PostgreSQL")));
                break;

            case "mssql":
                services.AddDbContext<TA>(m => m.UseSqlServer(e => e.MigrationsAssembly("Migrators.MSSQL")));
                break;

            case "mysql":
                services.AddDbContext<TA>(m => m.UseMySql(options.ConnectionString, ServerVersion.AutoDetect(options.ConnectionString), e =>
                {
                    e.MigrationsAssembly("Migrators.MySQL");
                    e.SchemaBehavior(MySqlSchemaBehavior.Ignore);
                }));
                break;
        }

        if (dbContext.Database.GetMigrations().Any())
        {
            if (dbContext.Database.GetPendingMigrations().Any())
            {
                dbContext.Database.Migrate();
                _logger.Information("Applying Root Migrations.");
            }

            if (dbContext.Database.CanConnect())
            {
                try
                {
                    SeedRootTenant(dbContext, options);
                    foreach (var tenant in dbContext.Tenants.ToList())
                    {
                        services.SetupTenantDatabase<TA>(options, tenant);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        return services;
    }

    private static IServiceCollection SetupTenantDatabase<TA>(this IServiceCollection services, DatabaseSettings options, Tenant tenant)
    where TA : ApplicationDbContext
    {
        string tenantConnectionString = string.IsNullOrEmpty(tenant.ConnectionString) ? options.ConnectionString : tenant.ConnectionString;
        switch (options.DBProvider.ToLower())
        {
            case "postgresql":
                services.AddDbContext<TA>(m => m.UseNpgsql(e => e.MigrationsAssembly("Migrators.PostgreSQL")));
                break;

            case "mssql":
                services.AddDbContext<TA>(m => m.UseSqlServer(e => e.MigrationsAssembly("Migrators.MSSQL")));
                break;

            case "mysql":
                services.AddDbContext<TA>(m => m.UseMySql(tenantConnectionString, ServerVersion.AutoDetect(tenantConnectionString), e =>
                {
                    e.MigrationsAssembly("Migrators.MySQL");
                    e.SchemaBehavior(MySqlSchemaBehavior.Ignore);
                }));
                break;
        }

        var scope = services.BuildServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TA>();
        var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetService<RoleManager<ApplicationRole>>();
        var seeders = scope.ServiceProvider.GetServices<IDatabaseSeeder>().ToList();
        TenantBootstrapper.Initialize(dbContext, options, tenant, userManager, roleManager, seeders);
        return services;
    }

    public static T GetOptions<T>(this IServiceCollection services, string sectionName)
    where T : new()
    {
        using var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var section = configuration.GetSection(sectionName);
        var options = new T();
        section.Bind(options);

        return options;
    }

    private static void SeedRootTenant<T>(T dbContext, DatabaseSettings options)
    where T : TenantManagementDbContext
    {
        if (!dbContext.Tenants.Any(t => t.Key == MultitenancyConstants.Root.Key))
        {
            var rootTenant = new Tenant(MultitenancyConstants.Root.Name, MultitenancyConstants.Root.Key, MultitenancyConstants.Root.EmailAddress, options.ConnectionString);
            rootTenant.SetValidity(DateTime.UtcNow.AddYears(1));
            dbContext.Tenants.Add(rootTenant);
            dbContext.SaveChanges();
        }
    }
}