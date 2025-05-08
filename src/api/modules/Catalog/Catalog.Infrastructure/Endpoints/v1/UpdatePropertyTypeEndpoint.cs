using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Update.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class UpdatePropertyTypeEndpoint
{
    internal static RouteHandlerBuilder MapPropertyTypeUpdateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/propertytypes/{id:guid}", async (Guid id, UpdatePropertyTypeCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdatePropertyTypeEndpoint))
            .WithSummary("Updates a PropertyType")
            .WithDescription("Updates a PropertyType")
            .Produces<UpdatePropertyTypeResponse>()
            .RequirePermission("Permissions.PropertyTypes.Update")
            .MapToApiVersion(1);
    }
}
