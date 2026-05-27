using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.Dtos;
using FSH.Modules.Catalog.Contracts.v1.Products.AddProductImage;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.AddProductImage;

public static class AddProductImageEndpoint
{
    internal static RouteHandlerBuilder MapAddProductImageEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/products/{productId:guid}/images",
                async (Guid productId, [FromBody] AddImageBody body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new AddProductImageCommand(productId, body.FileAssetId, body.Url),
                        ct)))
            .WithName("AddProductImage")
            .WithSummary("Attach an image to a product")
            .Produces<ProductImageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CatalogPermissions.Products.Update);

    // Body shape: ProductId comes from the route, so we only accept FileAssetId + Url in the body.
    public sealed record AddImageBody(Guid? FileAssetId, string Url);
}
