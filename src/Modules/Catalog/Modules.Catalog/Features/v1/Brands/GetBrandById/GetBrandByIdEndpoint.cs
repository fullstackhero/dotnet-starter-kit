using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Brands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Brands.GetBrandById;

public static class GetBrandByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetBrandByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/brands/{brandId:guid}",
                (Guid brandId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetBrandByIdQuery(brandId), ct))
            .WithName("GetBrandById")
            .WithSummary("Get a brand by id")
            .RequirePermission(CatalogPermissions.Brands.View);
    }
}
