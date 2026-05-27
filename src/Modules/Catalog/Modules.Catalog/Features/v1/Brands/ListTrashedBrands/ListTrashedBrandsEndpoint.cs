using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Brands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Brands.ListTrashedBrands;

public static class ListTrashedBrandsEndpoint
{
    internal static RouteHandlerBuilder MapListTrashedBrandsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/brands/trash",
                async (int? pageNumber, int? pageSize, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListTrashedBrandsQuery(pageNumber ?? 1, pageSize ?? 20), ct)))
            .WithName("ListTrashedBrands")
            .WithSummary("List soft-deleted brands")
            .RequirePermission(CatalogPermissions.Brands.Restore);
    }
}
