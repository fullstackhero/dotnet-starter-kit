using Finbuckle.MultiTenant;
using FSH.Framework.Core.MultiTenancy.Abstractions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Multitenancy.Services;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.Multitenancy;
internal static class Extensions
{
    public static IServiceCollection AddMultitenancy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<IConnectionStringValidator, ConnectionStringValidator>();
        services.BindDbContext<TenantDbContext, TenantDbBootstrapper>();
        services.AddMultiTenant<FshTenantInfo>()
            .WithHostStrategy()
            //.WithClaimStrategy(MultitenancyConstants.Identifier)
            .WithHeaderStrategy(MultitenancyConstants.Identifier)
            .WithEFCoreStore<TenantDbContext, FshTenantInfo>();
        services.AddScoped<ITenantService, TenantService>();
        return services;
    }

    public static IApplicationBuilder UseFshMultitenancy(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMultiTenant();
        using var scope = app.ApplicationServices.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<FshTenantInfo>>();
        var services = scope.ServiceProvider.GetServices<IDbBootstrapper>()!;

        //migrate primary database
        foreach (var db in services)
        {
            db.StartAsync(null, CancellationToken.None).Wait();
        }

        //get all other tenants
        var tenants = tenantStore.GetAllAsync().Result;
        var others = tenants.Where(a => a.Id != MultitenancyConstants.Root.Id).ToList();
        foreach (var tenant in others)
        {
            foreach (var db in services)
            {
                db.StartAsync(tenant, CancellationToken.None).Wait();
            }
        }
        return app;
    }
}
