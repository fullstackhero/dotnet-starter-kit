using Asp.Versioning;
using FSH.Framework.Infrastructure.Messaging.CQRS;
using FSH.Framework.Infrastructure.Modules;
using FSH.Framework.Tenant.Features.v1.ActivateTenant;
using FSH.Framework.Tenant.Features.v1.CreateTenant;
using FSH.Framework.Tenant.Features.v1.DisableTenant;
using FSH.Framework.Tenant.Features.v1.GetTenantById;
using FSH.Framework.Tenant.Features.v1.GetTenants;
using FSH.Framework.Tenant.Features.v1.UpgradeTenant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FSH.Framework.Tenant;
public class TenantModule : IModule
{
    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration config)
    {
        services.RegisterCommandAndQueryHandlers(Assembly.GetExecutingAssembly());

        // other registrations
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
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

        CreateTenantEndpoint.Map(group);
        DisableTenantEndpoint.Map(group);
        GetTenantByIdEndpoint.Map(group);
        GetTenantsEndpoint.Map(group);
        UpgradeTenantEndpoint.Map(group);
        ActivateTenantEndpoint.Map(group);

        return endpoints;
    }
}
