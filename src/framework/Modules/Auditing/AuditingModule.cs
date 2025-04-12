using Asp.Versioning;
using FSH.Framework.Auditing.Features.v1.GetUserTrails;
using FSH.Framework.Infrastructure.Messaging.CQRS;
using FSH.Framework.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Auditing.Endpoints;
public class AuditingModule : IModule
{
    public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration config)
    {
        services.RegisterCommandAndQueryHandlers(typeof(AuditingModule).Assembly);

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
            .MapGroup("api/v{version:apiVersion}/auditing")
            .WithTags("Auditing")
            .WithOpenApi()
            .WithApiVersionSet(apiVersionSet);

        GetUserTrailsEndpoint.Map(group);

        return endpoints;
    }
}
