using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Category.Features.Update.v1;
 
public static class UpdateCategoryItemEndpoint
{
    internal static RouteHandlerBuilder MapCategoryItemUpdationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.
            MapPut("/{id:guid}", async (Guid id, UpdateCategoryItemCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateCategoryItemEndpoint))
            .WithSummary("Updates a Category item")
            .WithDescription("Updated a Category item")
            .Produces<UpdateCategoryItemResponse>(StatusCodes.Status200OK)
            .RequirePermission("Permissions.CategoryItems.Update")
            .MapToApiVersion(new ApiVersion(1, 0));

    }
}
