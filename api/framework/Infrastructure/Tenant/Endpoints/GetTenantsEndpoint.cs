using FSH.Framework.Core.Tenant.Features.GetTenants;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Tenant.Endpoints;
public static class GetTenantsEndpoint
{
    internal static RouteHandlerBuilder MapGetTenantsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", (ISender mediator) => mediator.Send(new GetTenantsQuery()))
                                .WithName(nameof(GetTenantsEndpoint))
                                .WithSummary("get tenants")
                                .WithDescription("get tenants");
    }
}
