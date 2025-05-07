using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Agencies.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class GetAgencyEndpoint
{
    internal static RouteHandlerBuilder MapGetAgencyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetAgencyRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetAgencyEndpoint))
            .WithSummary("gets Agency by id")
            .WithDescription("gets Agency by id")
            .Produces<AgencyResponse>()
            .RequirePermission("Permissions.Agencies.View")
            .MapToApiVersion(1);
    }
}
