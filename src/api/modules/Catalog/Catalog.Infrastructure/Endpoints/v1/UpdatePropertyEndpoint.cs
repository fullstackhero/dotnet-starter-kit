using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Properties.Update.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class UpdatePropertyEndpoint
{
    internal static RouteHandlerBuilder MapPropertyUpdateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/propertytypes/{id:guid}", async (Guid id, UpdatePropertyCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdatePropertyEndpoint))
            .WithSummary("Updates a PropertyType")
            .WithDescription("Updates a PropertyType")
            .Produces<UpdatePropertyResponse>()
            .RequirePermission("Permissions.PropertyTypes.Update")
            .MapToApiVersion(1);
    }
}
