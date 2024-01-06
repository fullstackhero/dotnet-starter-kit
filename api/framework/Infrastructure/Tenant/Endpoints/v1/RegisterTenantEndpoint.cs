using FSH.Framework.Core.Tenant.Features.v1.RegisterTenant;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Tenant.Endpoints.v1;
public static class RegisterTenantEndpoint
{
    internal static RouteHandlerBuilder MapRegisterTenantEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", (RegisterTenantCommand request, ISender mediator) => mediator.Send(request))
                                .WithName(nameof(MapRegisterTenantEndpoint))
                                .WithSummary("creates a tenant")
                                .WithDescription("creates a tenant")
                                .MapToApiVersion(1.0);
    }
}
