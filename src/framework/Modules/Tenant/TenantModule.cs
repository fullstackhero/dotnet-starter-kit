using Asp.Versioning;
using FSH.Framework.Infrastructure.Messaging.CQRS;
using FSH.Framework.Infrastructure.Modules;
using FSH.Framework.Tenant.Features.v1;
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

        ActivateTenant.MapEndpoint(group);
        CreateTenant.MapEndpoint(group);
        DisableTenant.MapEndpoint(group);
        GetTenantById.MapEndpoint(group);
        GetTenants.MapEndpoint(group);
        UpgradeSubscription.MapEndpoint(group);

        return endpoints;
    }
}
