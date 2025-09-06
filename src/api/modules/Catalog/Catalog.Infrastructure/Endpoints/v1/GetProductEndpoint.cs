using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Products.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class GetProductEndpoint
{
    internal static RouteHandlerBuilder MapGetProductEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetProductRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetProductEndpoint))
            .WithSummary("gets product by id")
            .WithDescription("gets prodct by id")
            .Produces<ProductResponse>()
            .RequirePermission("Permissions.Products.View")
            .MapToApiVersion(1);
    }
}
