using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Categories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Categories.ListTrashedCategories;

public static class ListTrashedCategoriesEndpoint
{
    internal static RouteHandlerBuilder MapListTrashedCategoriesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/categories/trash",
                async (int? pageNumber, int? pageSize, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListTrashedCategoriesQuery(pageNumber ?? 1, pageSize ?? 20), ct)))
            .WithName("ListTrashedCategories")
            .WithSummary("List soft-deleted categories")
            .RequirePermission(CatalogPermissions.Categories.Restore);
    }
}
