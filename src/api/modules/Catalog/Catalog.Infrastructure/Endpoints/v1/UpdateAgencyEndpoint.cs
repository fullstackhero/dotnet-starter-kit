using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Agencies.Update.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class UpdateAgencyEndpoint
{
    internal static RouteHandlerBuilder MapAgencyUpdateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/{id:guid}", async (Guid id, UpdateAgencyCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateAgencyEndpoint))
            .WithSummary("update a Agency")
            .WithDescription("update a Agency")
            .Produces<UpdateAgencyResponse>()
            .RequirePermission("Permissions.Agencies.Update")
            .MapToApiVersion(1);
    }
}
