using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Agencies.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class DeleteAgencyEndpoint
{
    internal static RouteHandlerBuilder MapAgencyDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
             {
                 await mediator.Send(new DeleteAgencyCommand(id));
                 return Results.NoContent();
             })
            .WithName(nameof(DeleteAgencyEndpoint))
            .WithSummary("deletes Agency by id")
            .WithDescription("deletes Agency by id")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.Agencies.Delete")
            .MapToApiVersion(1);
    }
}
