using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Domain.Multitenancy;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace DN.WebApi.Infrastructure.Multitenancy;

public static class DatabaseInitializer
{
    private static readonly ILogger _logger = Log.ForContext(typeof(DatabaseInitializer));

    public static void InitializeDatabases(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TenantManagementDbContext>();

        if (dbContext.Database.GetPendingMigrations().Any())
        {
            _logger.Information("Applying Root Migrations.");
            dbContext.Database.Migrate();
        }

        var dbSettings = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;

        SeedRootTenant(dbContext, dbSettings.ConnectionString!);

        foreach (var tenant in dbContext.Tenants.ToList())
        {
            InitializeTenantDatabase(serviceProvider, dbSettings.DBProvider!, dbSettings.ConnectionString!, tenant);
        }

        _logger.Information("For documentations and guides, visit https://www.fullstackhero.net");
        _logger.Information("To Sponsor this project, visit https://opencollective.com/fullstackhero");
    }

    private static void InitializeTenantDatabase(IServiceProvider serviceProvider, string dbProvider, string rootConnectionString, Tenant tenant)
    {
        if (tenant.Key is null)
        {
            throw new InvalidOperationException("Invalid tenant key.");
        }

        using var scope = serviceProvider.CreateScope();

        // First set current tenant so the right connectionstring is used
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
        tenantService.SetCurrentTenant(tenant.Key);

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var seeders = scope.ServiceProvider.GetServices<IDatabaseSeeder>().ToList();
        TenantBootstrapper.Initialize(dbContext, dbProvider, rootConnectionString, tenant, userManager, roleManager, seeders);
    }

    private static void SeedRootTenant(TenantManagementDbContext dbContext, string connectionString)
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
}