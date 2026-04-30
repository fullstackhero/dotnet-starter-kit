using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Brands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Brands.UpdateBrand;

public static class UpdateBrandEndpoint
{
    internal static RouteHandlerBuilder MapUpdateBrandEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/brands/{brandId:guid}",
                async (Guid brandId, UpdateBrandCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { BrandId = brandId };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("UpdateBrand")
            .WithSummary("Update a brand")
            .RequirePermission(CatalogPermissions.Brands.Update);
    }
}
