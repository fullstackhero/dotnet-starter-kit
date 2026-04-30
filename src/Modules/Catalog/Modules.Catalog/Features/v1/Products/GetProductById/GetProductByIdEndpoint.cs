using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.GetProductById;

public static class GetProductByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetProductByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/products/{productId:guid}",
                (Guid productId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetProductByIdQuery(productId), ct))
            .WithName("GetProductById")
            .WithSummary("Get a product by id")
            .RequirePermission(CatalogPermissions.Products.View);
    }
}
