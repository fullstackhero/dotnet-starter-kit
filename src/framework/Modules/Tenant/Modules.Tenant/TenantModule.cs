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
using FSH.Framework.Tenant;
using FSH.Framework.Tenant.Data;
using FSH.Framework.Tenant.Features.v1.CreateTenant;
using FSH.Framework.Tenant.Features.v1.DisableTenant;
using FSH.Framework.Tenant.Features.v1.GetTenantById;
using FSH.Framework.Tenant.Features.v1.GetTenants;
using FSH.Framework.Tenant.Features.v1.UpgradeTenant;
using FSH.Framework.Tenant.Services;
using FSH.Modules.Common.Infrastructure.Modules;
using FSH.Modules.Common.Shared.Constants;
using FSH.Modules.Tenant.Features.v1.ActivateTenant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FSH.Modules.Tenant;

public class TenantModule : IModule
{
    public void AddModule(IServiceCollection services, IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RegisterCommandAndQueryHandlers(Assembly.GetExecutingAssembly());

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);

        services.AddTransient<IConnectionStringValidator, ConnectionStringValidator>();

        services.BindDbContext<TenantDbContext>();

        services
            .AddMultiTenant<FshTenantInfo>(options =>
            {
                options.Events.OnTenantResolveCompleted = async context =>
                {
                    if (context.MultiTenantContext.StoreInfo is null) return;
                    if (context.MultiTenantContext.StoreInfo.StoreType != typeof(DistributedCacheStore<FshTenantInfo>))
                    {
                        var sp = ((HttpContext)context.Context!).RequestServices;
                        var distributedStore = sp
                            .GetRequiredService<IEnumerable<IMultiTenantStore<FshTenantInfo>>>()
                            .FirstOrDefault(s => s.GetType() == typeof(DistributedCacheStore<FshTenantInfo>));

                        await distributedStore!.TryAddAsync(context.MultiTenantContext.TenantInfo!);
                    }
                    await Task.CompletedTask;
                };
            })
            .WithClaimStrategy(FshClaims.Tenant)
            .WithHeaderStrategy(MutiTenancyConstants.Identifier)
            .WithDelegateStrategy(async context =>
            {
                if (context is not HttpContext httpContext) return null;

                if (!httpContext.Request.Query.TryGetValue("tenant", out var tenantIdentifier) ||
                    string.IsNullOrEmpty(tenantIdentifier))
                    return null;

                return await Task.FromResult(tenantIdentifier.ToString());
            })
            .WithDistributedCacheStore(TimeSpan.FromMinutes(60))
            .WithEFCoreStore<TenantDbContext, FshTenantInfo>();

        services.AddScoped<ITenantService, TenantService>();
    }

    public void ConfigureModule(WebApplication app)
    {
        app.ConfigureMultiTenantDatabases();

        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("api/v{version:apiVersion}/tenants")
            .WithTags("Tenants")
            .WithOpenApi()
            .WithApiVersionSet(versionSet);

        DisableTenantEndpoint.Map(group);
        GetTenantByIdEndpoint.Map(group);
        GetTenantsEndpoint.Map(group);
        UpgradeTenantEndpoint.Map(group);
        ActivateTenantEndpoint.Map(group);
        CreateTenantEndpoint.Map(group);
    }
}
