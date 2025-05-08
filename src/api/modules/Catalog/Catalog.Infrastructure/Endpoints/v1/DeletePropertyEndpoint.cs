using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Properties.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class DeletePropertyEndpoint
{
    internal static RouteHandlerBuilder MapPropertyDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/properties/{id:guid}", async (Guid id, ISender mediator) =>
            {
                await mediator.Send(new DeletePropertyCommand(id));
                return Results.NoContent();
            })
            .WithName(nameof(DeletePropertyEndpoint))
            .WithSummary("Deletes a Property")
            .WithDescription("Deletes a Property")
            .RequirePermission("Permissions.Properties.Delete")
            .MapToApiVersion(1);
    }
}
