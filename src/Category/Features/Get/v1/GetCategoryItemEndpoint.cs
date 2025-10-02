using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Category.Features.Get.v1;
 
public static class GetCategoryItemEndpoint
{
    internal static RouteHandlerBuilder MapGetCategoryItemEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var response = await mediator.Send(new GetCategoryItemRequest(id));
            return Results.Ok(response);
        })
                        .WithName(nameof(GetCategoryItemEndpoint))
                        .WithSummary("gets category item by id")
                        .WithDescription("gets category item by id")
                        .Produces<GetCategoryItemResponse>()
                        .RequirePermission("Permissions.CategoryItems.View")
                        .MapToApiVersion(1);
    }
}
