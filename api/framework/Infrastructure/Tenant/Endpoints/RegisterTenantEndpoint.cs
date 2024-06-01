using FSH.Framework.Core.Tenant.Features.RegisterTenant;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Tenant.Endpoints;
public static class RegisterTenantEndpoint
{
    internal static RouteHandlerBuilder MapRegisterTenantEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", (RegisterTenantCommand request, ISender mediator) => mediator.Send(request))
                                .WithName(nameof(MapRegisterTenantEndpoint))
                                .WithSummary("creates a tenant")
                                .RequirePermission("Permissions.Tenants.Create")
                                .WithDescription("creates a tenant");
    }
}
