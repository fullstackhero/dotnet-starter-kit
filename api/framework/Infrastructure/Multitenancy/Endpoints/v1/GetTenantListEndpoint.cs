using FSH.Framework.Core.MultiTenancy.Features.GetList;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Multitenancy.Endpoints.v1;
public static class GetTenantListEndpoint
{
    internal static RouteHandlerBuilder MapGetTenantListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", (ISender mediator) => mediator.Send(new GetTenantListRquest()))
                        .WithName(nameof(GetTenantListEndpoint))
                        .WithSummary("gets all tenant")
                        .WithDescription("gets all tenant")
                        .MapToApiVersion(1.0);
    }
}
