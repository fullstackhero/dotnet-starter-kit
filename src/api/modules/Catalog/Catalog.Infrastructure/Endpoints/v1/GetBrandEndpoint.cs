using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Brands.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class GetBrandEndpoint
{
    internal static RouteHandlerBuilder MapGetBrandEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetBrandRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetBrandEndpoint))
            .WithSummary("gets brand by id")
            .WithDescription("gets brand by id")
            .Produces<BrandResponse>()
            .RequirePermission("Permissions.Brands.View")
            .MapToApiVersion(1);
    }
}
