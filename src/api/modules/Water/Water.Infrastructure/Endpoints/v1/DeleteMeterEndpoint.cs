using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Meters.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class DeleteMeterEndpoint
{
    internal static RouteHandlerBuilder MapMeterDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
             {
                 await mediator.Send(new DeleteMeterCommand(id));
                 return Results.NoContent();
             })
            .WithName(nameof(DeleteMeterEndpoint))
            .WithSummary("deletes meter by id")
            .WithDescription("deletes meter by id")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.Meters.Delete")
            .MapToApiVersion(1);
    }
}
