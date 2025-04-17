using Asp.Versioning;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.Stores.DistributedCacheStore;
using FluentValidation;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Messaging.CQRS;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.Persistence.Services;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Tenant.Data;
using FSH.Framework.Tenant.Features.v1.CreateTenant;
using FSH.Framework.Tenant.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Reflection;

namespace FSH.Framework.Tenant;
public static class TenantModule
{
    public static IServiceCollection ConfigureTenantModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.RegisterCommandAndQueryHandlers(Assembly.GetExecutingAssembly());

        var assemblies = new Assembly[]
        {
            typeof(TenantModule).Assembly
        };
        services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true);

        services.AddTransient<IConnectionStringValidator, ConnectionStringValidator>();
        services.BindDbContext<TenantDbContext>();
        services
            .AddMultiTenant<FshTenantInfo>(config =>
            {
                // to save database calls to resolve tenant
                // this was happening for every request earlier, leading to ineffeciency
                config.Events.OnTenantResolveCompleted = async (context) =>
                {
                    if (context.MultiTenantContext.StoreInfo is null) return;
                    if (context.MultiTenantContext.StoreInfo.StoreType != typeof(DistributedCacheStore<FshTenantInfo>))
                    {
                        var sp = ((HttpContext)context.Context!).RequestServices;
                        var distributedCacheStore = sp
                            .GetService<IEnumerable<IMultiTenantStore<FshTenantInfo>>>()!
                            .FirstOrDefault(s => s.GetType() == typeof(DistributedCacheStore<FshTenantInfo>));

                        await distributedCacheStore!.TryAddAsync(context.MultiTenantContext.TenantInfo!);
                    }
                    await Task.FromResult(0);
                };
            })
            .WithClaimStrategy(FshClaims.Tenant)
            .WithHeaderStrategy(TenantConstants.Identifier)
            .WithDelegateStrategy(async context =>
            {
                if (context is not HttpContext httpContext)
                    return null;
                if (!httpContext.Request.Query.TryGetValue("tenant", out var tenantIdentifier) || string.IsNullOrEmpty(tenantIdentifier))
                    return null;
                return await Task.FromResult(tenantIdentifier.ToString());
            })
            .WithDistributedCacheStore(TimeSpan.FromMinutes(60))
            .WithEFCoreStore<TenantDbContext, FshTenantInfo>();
        services.AddScoped<ITenantService, TenantService>();
        return services;
    }
    public static WebApplication UseFshMultiTenancy(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMultiTenant();

        // set up tenant store
        var tenants = TenantStoreSetup(app);

        // set up tenant databases
        app.SetupTenantDatabases(tenants);

        // register endpoints
        app.MapTenantEndpoints();
        return app;
    }

    private static IApplicationBuilder SetupTenantDatabases(this IApplicationBuilder app, IEnumerable<FshTenantInfo> tenants)
    {
        foreach (var tenant in tenants)
        {
            // create a scope for tenant
            using var tenantScope = app.ApplicationServices.CreateScope();

            //set current tenant so that the right connection string is used
            tenantScope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
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
        var scope = app.ApplicationServices.CreateScope();

        // tenant master schema migration
        var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        if (tenantDbContext.Database.GetPendingMigrations().Any())
        {
            tenantDbContext.Database.Migrate();
            Log.Information("applied database migrations for tenant module");
        }

        // default tenant seeding
        if (tenantDbContext.TenantInfo.Find(TenantConstants.Root.Id) is null)
        {
            var rootTenant = new FshTenantInfo(
                TenantConstants.Root.Id,
                TenantConstants.Root.Name,
                string.Empty,
                TenantConstants.Root.EmailAddress);

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

    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/tenants")
            .WithTags("Tenants")
            .WithOpenApi()
            .WithApiVersionSet(apiVersionSet);

        CreateTenantEndpoint.Map(group).AllowAnonymous();
        //DisableTenantEndpoint.Map(group);
        //GetTenantByIdEndpoint.Map(group);
        //GetTenantsEndpoint.Map(group);
        //UpgradeTenantEndpoint.Map(group);
        //ActivateTenantEndpoint.Map(group);

        return endpoints;
    }
}