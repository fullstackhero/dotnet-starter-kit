using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class CreatePropertyTypeEndpoint
{
    internal static RouteHandlerBuilder MapPropertyTypeCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/propertytypes", async (CreatePropertyTypeCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreatePropertyTypeEndpoint))
            .WithSummary("Creates a PropertyType")
            .WithDescription("Creates a PropertyType")
            .Produces<CreatePropertyTypeResponse>()
            .RequirePermission("Permissions.PropertyTypes.Create")
            .MapToApiVersion(1);
    }
}
