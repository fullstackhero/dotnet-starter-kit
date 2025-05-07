using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Agencies.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class CreateAgencyEndpoint
{
    internal static RouteHandlerBuilder MapAgencyCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/", async (CreateAgencyCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreateAgencyEndpoint))
            .WithSummary("creates a Agency")
            .WithDescription("creates a Agency")
            .Produces<CreateAgencyResponse>()
            .RequirePermission("Permissions.Agencies.Create")
            .MapToApiVersion(1);
    }
}
