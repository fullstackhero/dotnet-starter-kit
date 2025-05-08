using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class DeletePropertyTypeEndpoint
{
    internal static RouteHandlerBuilder MapPropertyTypeDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/propertytypes/{id:guid}", async (Guid id, ISender mediator) =>
            {
                await mediator.Send(new DeletePropertyTypeCommand(id));
                return Results.NoContent();
            })
            .WithName(nameof(DeletePropertyTypeEndpoint))
            .WithSummary("Deletes a PropertyType by ID")
            .WithDescription("Deletes a PropertyType by ID")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.PropertyTypes.Delete")
            .MapToApiVersion(1);
    }
}
