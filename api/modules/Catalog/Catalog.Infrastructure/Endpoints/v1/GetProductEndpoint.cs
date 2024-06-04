using FSH.WebApi.Catalog.Application.Products.Get.v1;
using FSH.Framework.Infrastructure.Auth.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using MediatR;

namespace FSH.WebApi.Catalog.Infrastructure.Endpoints.v1;
public static class GetProductEndpoint
{
    internal static RouteHandlerBuilder MapGetProductEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/{id:guid}", (Guid id, ISender mediator) => mediator.Send(new GetProductRequest(id)))
                        .WithName(nameof(GetProductEndpoint))
                        .WithSummary("gets product by id")
                        .WithDescription("gets prodct by id")
                        .Produces<GetProductResponse>()
                        .RequirePermission("Permissions.Products.View")
                        .MapToApiVersion(1);
    }
}
