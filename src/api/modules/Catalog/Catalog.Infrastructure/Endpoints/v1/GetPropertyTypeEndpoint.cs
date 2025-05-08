using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class GetPropertyTypeEndpoint
{
    internal static RouteHandlerBuilder MapGetPropertyTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/propertytypes/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetPropertyTypeRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetPropertyTypeEndpoint))
            .WithSummary("Gets a PropertyType by ID")
            .WithDescription("Gets a PropertyType by ID")
            .Produces<PropertyTypeResponse>()
            .RequirePermission("Permissions.PropertyTypes.View")
            .MapToApiVersion(1);
    }
}
