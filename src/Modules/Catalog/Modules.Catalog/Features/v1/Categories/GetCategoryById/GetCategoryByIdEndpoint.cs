using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Categories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Categories.GetCategoryById;

public static class GetCategoryByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetCategoryByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/categories/{categoryId:guid}",
                (Guid categoryId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetCategoryByIdQuery(categoryId), ct))
            .WithName("GetCategoryById")
            .WithSummary("Get a category by id")
            .RequirePermission(CatalogPermissions.Categories.View);
    }
}
