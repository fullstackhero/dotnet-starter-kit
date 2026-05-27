using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Categories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Categories.SearchCategories;

public static class SearchCategoriesEndpoint
{
    internal static RouteHandlerBuilder MapSearchCategoriesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/categories",
                (string? search, Guid? parentCategoryId, int pageNumber, int pageSize,
                 string? sortBy, string? sortDir,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new SearchCategoriesQuery(
                            search,
                            parentCategoryId,
                            pageNumber == 0 ? 1 : pageNumber,
                            pageSize == 0 ? 50 : pageSize,
                            sortBy,
                            sortDir),
                        ct))
            .WithName("SearchCategories")
            .WithSummary("Search categories (paged, filter by parent, sortable)")
            .RequirePermission(CatalogPermissions.Categories.View);
    }
}
