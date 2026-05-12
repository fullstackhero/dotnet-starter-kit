using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.ReorderProductImages;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.ReorderProductImages;

public static class ReorderProductImagesEndpoint
{
    internal static RouteHandlerBuilder MapReorderProductImagesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/products/{productId:guid}/images/order",
                async (Guid productId, [FromBody] ReorderBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ReorderProductImagesCommand(productId, body.OrderedImageIds), ct);
                    return Results.NoContent();
                })
            .WithName("ReorderProductImages")
            .WithSummary("Set the sort order of a product's images")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CatalogPermissions.Products.Update);

    public sealed record ReorderBody(IReadOnlyList<Guid> OrderedImageIds);
}
