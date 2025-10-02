using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSH.Framework.Core.Paging;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Category.Features.GetList.v1;
 
public static class GetCategoryItemListEndpoint
{
    internal static RouteHandlerBuilder MapGetCategoryItemListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/search", async (ISender mediator, [FromBody] PaginationFilter filter) =>
        {
            var response = await mediator.Send(new GetCategoryItemListRequest(filter));
            return Results.Ok(response);
        })
        .WithName(nameof(GetCategoryItemListEndpoint))
        .WithSummary("Gets a list of Category items with paging support")
        .WithDescription("Gets a list of Category items with paging support")
        .Produces<PagedList<CategoryItemDto>>()
        .RequirePermission("Permissions.CategoryItems.View")
        .MapToApiVersion(1);
    }
}
