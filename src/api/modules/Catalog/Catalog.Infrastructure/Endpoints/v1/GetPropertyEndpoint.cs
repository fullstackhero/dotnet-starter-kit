using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Properties.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class GetPropertyEndpoint
{
    internal static RouteHandlerBuilder MapGetPropertyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/properties/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetPropertyRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetPropertyEndpoint))
            .WithSummary("Gets a Property by ID")
            .WithDescription("Gets a Property by ID")
            .Produces<PropertyResponse>()
            .RequirePermission("Permissions.Properties.View")
            .MapToApiVersion(1);
    }
}
