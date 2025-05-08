using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Properties.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class CreatePropertyEndpoint
{
    internal static RouteHandlerBuilder MapPropertyCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/properties", async (CreatePropertyCommand command, ISender mediator) =>
            {
                var response = await mediator.Send(command);
                return Results.Created($"/properties/{response.Id}", response);
            })
            .WithName(nameof(CreatePropertyEndpoint))
            .WithSummary("Creates a new Property")
            .WithDescription("Creates a new Property")
            .Produces<CreatePropertyResponse>()
            .RequirePermission("Permissions.Properties.Create")
            .MapToApiVersion(1);
    }
}