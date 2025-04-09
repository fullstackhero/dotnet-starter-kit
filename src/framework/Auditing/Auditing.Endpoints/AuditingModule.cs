using System.Reflection;
using FSH.Framework.Auditing.Endpoints.v1.GetUserAudits;
using FSH.Framework.Infrastructure.Messaging.CQRS;
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
        // v1 endpoints
        endpoints.MapGetUserAuditsEndpoint();

        return endpoints;
    }
}
