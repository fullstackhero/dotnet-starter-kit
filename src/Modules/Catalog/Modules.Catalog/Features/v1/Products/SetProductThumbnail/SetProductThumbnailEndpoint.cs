using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.SetProductThumbnail;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.SetProductThumbnail;

public static class SetProductThumbnailEndpoint
{
    internal static RouteHandlerBuilder MapSetProductThumbnailEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/products/{productId:guid}/images/{imageId:guid}/thumbnail",
                async (Guid productId, Guid imageId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new SetProductThumbnailCommand(productId, imageId), ct);
                    return Results.NoContent();
                })
            .WithName("SetProductThumbnail")
            .WithSummary("Promote a product image to thumbnail (cover)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CatalogPermissions.Products.Update);
}
