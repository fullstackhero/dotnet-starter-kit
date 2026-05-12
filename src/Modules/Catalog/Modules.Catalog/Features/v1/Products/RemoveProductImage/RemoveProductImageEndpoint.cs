using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.RemoveProductImage;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.RemoveProductImage;

public static class RemoveProductImageEndpoint
{
    internal static RouteHandlerBuilder MapRemoveProductImageEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/products/{productId:guid}/images/{imageId:guid}",
                async (Guid productId, Guid imageId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new RemoveProductImageCommand(productId, imageId), ct);
                    return Results.NoContent();
                })
            .WithName("RemoveProductImage")
            .WithSummary("Detach an image from a product")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CatalogPermissions.Products.Update);
}
