using Finbuckle.MultiTenant;
using FSH.Framework.Core.MultiTenancy.Abstractions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Multitenancy.Services;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace FSH.Framework.Infrastructure.Multitenancy;
internal static class Extensions
{
    public static IServiceCollection AddMultitenancy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<IConnectionStringValidator, ConnectionStringValidator>();
        services.BindDbContext<TenantDbContext>();
        services.AddMultiTenant<FshTenantInfo>()
            .WithHostStrategy()
            //.WithClaimStrategy(MultitenancyConstants.Identifier)
            .WithHeaderStrategy(MultitenancyConstants.Identifier)
            .WithEFCoreStore<TenantDbContext, FshTenantInfo>();
        services.AddScoped<ITenantService, TenantService>();
        return services;
    }

    public static IApplicationBuilder UseMultitenancy(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMultiTenant();

        // set up tenant store
        var tenants = TenantStoreSetup(app);

        // set up tenant databases
        app.SetupTenantDatabases(tenants);

        return app;
    }

    private static IApplicationBuilder SetupTenantDatabases(this IApplicationBuilder app, IEnumerable<FshTenantInfo> tenants)
    {
        foreach (var tenant in tenants)
        {
            // create a scope for tenant
            using var tenantScope = app.ApplicationServices.CreateScope();

            //set current tenant so that the right connection string is used
            tenantScope.ServiceProvider.GetRequiredService<IMultiTenantContextAccessor>()
                .MultiTenantContext = new MultiTenantContext<FshTenantInfo>()
                {
                    TenantInfo = tenant
                };

            // using the scope, perform migrations / seeding
            var initializers = tenantScope.ServiceProvider.GetServices<IDbInitializer>();
            foreach (var initializer in initializers)
            {
                initializer.MigrateAsync(CancellationToken.None).Wait();
                initializer.SeedAsync(CancellationToken.None).Wait();
            }
        }
        return app;
    }

    private static IEnumerable<FshTenantInfo> TenantStoreSetup(IApplicationBuilder app)
    {
        // get db config
        var config = app.ApplicationServices.GetRequiredService<IOptions<DbConfig>>().Value;
        var scope = app.ApplicationServices.CreateScope();

        // tenant master schema migration
        var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        if (!config.UseInMemoryDb && tenantDbContext.Database.GetPendingMigrations().Any())
        {
            tenantDbContext.Database.Migrate();
            Log.Information("applied database migrations for tenant module");
        }

        // default tenant seeding
        if (tenantDbContext.TenantInfo.Find(MultitenancyConstants.Root.Id) is null)
        {
            var rootTenant = new FshTenantInfo(
                MultitenancyConstants.Root.Id,
                MultitenancyConstants.Root.Name,
                string.Empty,
                MultitenancyConstants.Root.EmailAddress);

            rootTenant.SetValidity(DateTime.UtcNow.AddYears(1));
            tenantDbContext.TenantInfo.Add(rootTenant);
            tenantDbContext.SaveChanges();
            Log.Information("configured default tenant data");
        }

        // get all tenants from store
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<FshTenantInfo>>();
        var tenants = tenantStore.GetAllAsync().Result;

        //dispose scope
        scope.Dispose();

        return tenants;
    }
}
