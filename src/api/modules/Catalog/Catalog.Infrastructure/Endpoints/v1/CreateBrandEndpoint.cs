using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Brands.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class CreateBrandEndpoint
{
    internal static RouteHandlerBuilder MapBrandCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/", async (CreateBrandCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreateBrandEndpoint))
            .WithSummary("creates a brand")
            .WithDescription("creates a brand")
            .Produces<CreateBrandResponse>()
            .RequirePermission("Permissions.Brands.Create")
            .MapToApiVersion(1);
    }
}
