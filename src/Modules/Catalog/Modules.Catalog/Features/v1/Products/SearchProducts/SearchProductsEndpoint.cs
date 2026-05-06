using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.SearchProducts;

public static class SearchProductsEndpoint
{
    internal static RouteHandlerBuilder MapSearchProductsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/products",
                (string? search, Guid? brandId, Guid? categoryId, bool? isActive,
                 int pageNumber, int pageSize, string? sortBy, string? sortDir,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new SearchProductsQuery(
                            search,
                            brandId,
                            categoryId,
                            isActive,
                            pageNumber == 0 ? 1 : pageNumber,
                            pageSize == 0 ? 20 : pageSize,
                            sortBy,
                            sortDir),
                        ct))
            .WithName("SearchProducts")
            .WithSummary("Search products (paged, filter by brand/category/active, sortable)")
            .RequirePermission(CatalogPermissions.Products.View);
    }
}
