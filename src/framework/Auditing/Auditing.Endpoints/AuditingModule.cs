using System.Reflection;
using Asp.Versioning;
using Asp.Versioning.Builder;
using FSH.Framework.Auditing.Endpoints.v1;
using FSH.Framework.Infrastructure.Messaging.CQRS;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Auditing.Endpoints;

public static class AuditingModule
{
    public static IServiceCollection AddAuditingModule(this IServiceCollection services)
    {
        services.RegisterCommandAndQueryHandlers(Assembly.GetExecutingAssembly());

        // Add infrastructure, services, mappings, etc.

        return services;
    }

    public static IEndpointRouteBuilder MapAuditingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ApiVersionSet apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        RouteGroupBuilder group = endpoints
            .MapGroup("api/v{version:apiVersion}/auditing")
            .WithTags("Auditing")
            .WithOpenApi()
            .WithApiVersionSet(apiVersionSet);

        // v1 endpoints
        GetUserAudits.MapEndpoint(group);

        return endpoints;
    }
}
