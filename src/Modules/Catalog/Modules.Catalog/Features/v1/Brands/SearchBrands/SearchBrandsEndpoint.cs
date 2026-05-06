using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Brands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Brands.SearchBrands;

public static class SearchBrandsEndpoint
{
    internal static RouteHandlerBuilder MapSearchBrandsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/brands",
                (string? search, int pageNumber, int pageSize, string? sortBy, string? sortDir,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new SearchBrandsQuery(
                            search,
                            pageNumber == 0 ? 1 : pageNumber,
                            pageSize == 0 ? 20 : pageSize,
                            sortBy,
                            sortDir),
                        ct))
            .WithName("SearchBrands")
            .WithSummary("Search brands (paged, sortable)")
            .RequirePermission(CatalogPermissions.Brands.View);
    }
}
