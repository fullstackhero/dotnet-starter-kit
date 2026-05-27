using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.AdjustProductStock;

public static class AdjustProductStockEndpoint
{
    internal static RouteHandlerBuilder MapAdjustProductStockEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPatch("/products/{productId:guid}/stock",
                async (Guid productId, AdjustProductStockCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { ProductId = productId };
                    return Results.Ok(new { stock = await mediator.Send(command, ct) });
                })
            .WithName("AdjustProductStock")
            .WithSummary("Adjust product stock by a delta (+/-)")
            .RequirePermission(CatalogPermissions.Products.AdjustStock);
    }
}
